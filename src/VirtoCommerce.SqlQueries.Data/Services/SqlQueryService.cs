using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.SqlQueries.Core.Events;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;
using VirtoCommerce.SqlQueries.Data.Models;
using VirtoCommerce.SqlQueries.Data.Repositories;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class SqlQueryService(
    Func<ISqlQueriesRepository> repositoryFactory,
    IPlatformMemoryCache platformMemoryCache,
    IEventPublisher eventPublisher,
    IEnumerable<ISqlQueryReportGenerator> generators,
    IConfiguration configuration)
    : CrudService<SqlQuery, SqlQueryEntity, SqlQueryChangingEvent, SqlQueryChangedEvent>(repositoryFactory, platformMemoryCache, eventPublisher), ISqlQueryService
{
    private const string SqlQueryConnectionStringPrefix = "SqlQueries.";

    protected override Task<IList<SqlQueryEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
    {
        return ((ISqlQueriesRepository)repository).GetSqlQueriesByIdsAsync(ids);
    }

    public virtual async Task<SqlQueryReport> GenerateReport(SqlQuery query, IList<SqlQueryParameter> parameters, string format, SqlQueryReportContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!query.Parameters.IsNullOrEmpty())
        {
            FillParameters(query, parameters);
        }

        var dataTable = new DataTable();

        using var dbContext = GetDbContext(query.ConnectionStringName);
        using var connection = dbContext.Database.GetDbConnection();

        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query.Query;

        if (!query.Parameters.IsNullOrEmpty())
        {
            foreach (var parameter in query.Parameters)
            {
                AddDatabaseParameter(parameter, command);
            }
        }

        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);

        var generator = generators.FirstOrDefault(x => x.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

        return generator == null
            ? throw new NotSupportedException($"Report format '{format}' is not supported.")
            : generator.GenerateReport(dataTable, context);
    }

    public virtual async Task<SqlQueryExecuteResult> ExecuteQuery(SqlQueryExecuteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConnectionStringName);

        var dbInfo = GetDatabaseInformation();
        if (!dbInfo.ConnectionStringNames.Contains(request.ConnectionStringName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Connection string '{request.ConnectionStringName}' is not available.");
        }

        const int maxRowCeiling = 1000;
        var maxRows = Math.Clamp(request.MaxRows, 1, maxRowCeiling);

        var result = new SqlQueryExecuteResult();
        var stopwatch = Stopwatch.StartNew();

        using var dbContext = GetDbContext(request.ConnectionStringName);
        using var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = request.Query;
            command.CommandTimeout = 30;
            command.Transaction = transaction;

            if (!request.Parameters.IsNullOrEmpty())
            {
                foreach (var parameter in request.Parameters)
                {
                    AddDatabaseParameter(parameter, command);
                }
            }

            using var reader = await command.ExecuteReaderAsync();

            var fieldCount = reader.FieldCount;
            for (var i = 0; i < fieldCount; i++)
            {
                result.Columns.Add(new SqlQueryExecuteColumn
                {
                    Name = reader.GetName(i),
                    Type = reader.GetFieldType(i)?.Name ?? "String",
                });
            }

            var rows = new List<object[]>();
            var rowCount = 0;

            while (await reader.ReadAsync())
            {
                rowCount++;

                if (rowCount <= maxRows)
                {
                    var values = new object[fieldCount];
                    reader.GetValues(values);

                    for (var i = 0; i < values.Length; i++)
                    {
                        if (values[i] is DBNull)
                        {
                            values[i] = null;
                        }
                    }

                    rows.Add(values);
                }

                if (rowCount > maxRows)
                {
                    break;
                }
            }

            result.Rows = rows;
            result.TotalRowCount = rowCount;
            result.IsTruncated = rowCount > maxRows;
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    public virtual IList<string> GetFormats()
    {
        return generators.Select(x => x.Format).ToList();
    }

    public virtual DatabaseInformation GetDatabaseInformation()
    {
        var result = AbstractTypeFactory<DatabaseInformation>.TryCreateInstance();

        result.DatabaseProvider = GetDatabaseProvider();
        var connectionStrings = configuration.GetSection("ConnectionStrings").Get<Dictionary<string, string>>();

        result.ConnectionStringNames = connectionStrings
            .Where(x => x.Key.StartsWith(SqlQueryConnectionStringPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .ToList();

        return result;
    }

    protected virtual DbContext GetDbContext(string connectionStringName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>();

        var databaseProvider = GetDatabaseProvider();
        var connectionString = configuration.GetConnectionString(connectionStringName);

        switch (databaseProvider)
        {
            case "MySql":
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;
            case "PostgreSql":
                optionsBuilder.UseNpgsql(connectionString);
                break;
            default:
                optionsBuilder.UseSqlServer(connectionString);
                break;
        }

        return new DbContext(optionsBuilder.Options);
    }

    protected virtual void AddDatabaseParameter(SqlQueryParameter parameter, DbCommand dbCommand)
    {
        var dbParameter = dbCommand.CreateParameter();
        dbParameter.ParameterName = parameter.Name;
        dbParameter.Value = GetParameterValue(parameter) ?? DBNull.Value;
        dbCommand.Parameters.Add(dbParameter);
    }

    protected virtual object GetParameterValue(SqlQueryParameter parameter)
    {
        var result = parameter.Value;

        if (parameter.Type == "Integer")
        {
            result = Convert.ToInt32(parameter.Value);
        }
        else if (parameter.Type == "Decimal")
        {
            result = Convert.ToDecimal(parameter.Value);
        }
        else if (parameter.Type == "DateTime")
        {
            result = Convert.ToDateTime(parameter.Value);
        }
        else if (parameter.Type == "Boolean")
        {
            result = Convert.ToBoolean(parameter.Value);
        }

        return result;
    }

    private static void FillParameters(SqlQuery query, IList<SqlQueryParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            var existingParameter = query.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (existingParameter != null)
            {
                existingParameter.Value = parameter.Value;
            }
        }
    }

    private string GetDatabaseProvider()
    {
        return configuration.GetValue("DatabaseProvider", "SqlServer");
    }
}
