using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
    IConfiguration configuration) : CrudService<SqlQuery, SqlQueryEntity, SqlQueryChangingEvent, SqlQueryChangedEvent>(repositoryFactory, platformMemoryCache, eventPublisher), ISqlQueryService
{
    private const string SqlQueryConnectionStringPrefix = "SqlQueries.";

    protected override Task<IList<SqlQueryEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
    {
        return ((ISqlQueriesRepository)repository).GetSqlQueriesByIdsAsync(ids);
    }

    public virtual async Task<SqlQueryReport> GenerateReport(SqlQuery query, string format)
    {
        var dataTable = new DataTable();

        using var dbContext = GetDbContext(query.ConnectionStringName);
        using var connection = dbContext.Database.GetDbConnection();

        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query.Query;

        if (query.Parameters != null && query.Parameters.Any())
        {
            foreach (var parameter in query.Parameters)
            {
                AddDatabaseParameter(parameter, command);
            }
        }

        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);

        var generator = generators.FirstOrDefault(x => x.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
        if (generator == null)
        {
            throw new NotSupportedException($"Report format '{format}' is not supported.");
        }

        return generator.GenerateReport(dataTable);
    }

    public virtual IList<string> GetFormats()
    {
        return generators.Select(x => x.Format).ToList();
    }

    public virtual DatabaseInformation GetDatabaseInformation()
    {
        var result = AbstractTypeFactory<DatabaseInformation>.TryCreateInstance();

        result.DatabaseProvider = configuration.GetValue("DatabaseProvider", "SqlServer");
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

        var databaseProvider = configuration.GetValue("DatabaseProvider", "SqlServer");
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
        object result = parameter.Value;

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
}
