using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.ExportImport;

public sealed class SqlQueriesExportImport(
    ISqlQueryService sqlQueryService,
    ISqlQuerySearchService sqlQuerySearchService,
    JsonSerializer jsonSerializer)
{
    private const int BatchSize = 50;

    public async Task DoExportAsync(Stream outStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo { Description = "SQL queries are loading" };
        progressCallback(progressInfo);

        using var sw = new StreamWriter(outStream, leaveOpen: true);
        using var writer = new JsonTextWriter(sw);

        await writer.WriteStartObjectAsync();

        progressInfo.Description = "SQL queries are started to export";
        progressCallback(progressInfo);

        await writer.WritePropertyNameAsync("SqlQueries");
        await writer.WriteStartArrayAsync();

        var criteria = AbstractTypeFactory<SqlQuerySearchCriteria>.TryCreateInstance();
        criteria.Take = BatchSize;

        var processedCount = 0;

        for (criteria.Skip = 0; ; criteria.Skip += BatchSize)
        {
            var searchResult = await sqlQuerySearchService.SearchAsync(criteria);

            foreach (var query in searchResult.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var exportSqlQuery = ConvertToExportSqlQuery(query);
                jsonSerializer.Serialize(writer, exportSqlQuery);
                processedCount++;
            }

            await writer.FlushAsync();

            progressInfo.Description = $"{processedCount} of {searchResult.TotalCount} SQL queries have been exported";
            progressInfo.ProcessedCount = processedCount;
            progressInfo.TotalCount = searchResult.TotalCount;
            progressCallback(progressInfo);

            if (criteria.Skip + BatchSize >= searchResult.TotalCount)
            {
                break;
            }
        }

        await writer.WriteEndArrayAsync();
        await writer.WriteEndObjectAsync();
        await writer.FlushAsync();
    }

    public async Task DoImportAsync(Stream inputStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo();

        using var streamReader = new StreamReader(inputStream, leaveOpen: true);
        using var reader = new JsonTextReader(streamReader);

        while (await reader.ReadAsync())
        {
            if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "SqlQueries")
            {
                await ImportSqlQueriesArrayAsync(reader, progressInfo, progressCallback, cancellationToken);
            }
        }
    }

    private async Task ImportSqlQueriesArrayAsync(JsonTextReader reader, ExportImportProgressInfo progressInfo, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync() || reader.TokenType != JsonToken.StartArray)
        {
            return;
        }

        var processedCount = 0;
        var batch = new List<SqlQuery>(BatchSize);

        while (await reader.ReadAsync() && reader.TokenType != JsonToken.EndArray)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exportSqlQuery = jsonSerializer.Deserialize<SqlQueriesExportSqlQuery>(reader);
            if (exportSqlQuery == null)
            {
                continue;
            }

            batch.Add(ConvertToSqlQuery(exportSqlQuery));

            if (batch.Count >= BatchSize)
            {
                await sqlQueryService.SaveChangesAsync(batch);
                processedCount += batch.Count;
                batch.Clear();

                progressInfo.Description = $"{processedCount} SQL queries have been imported";
                progressInfo.ProcessedCount = processedCount;
                progressCallback(progressInfo);
            }
        }

        if (batch.Count > 0)
        {
            await sqlQueryService.SaveChangesAsync(batch);
            processedCount += batch.Count;

            progressInfo.Description = $"{processedCount} SQL queries have been imported";
            progressInfo.ProcessedCount = processedCount;
            progressCallback(progressInfo);
        }
    }

    private static SqlQueriesExportSqlQuery ConvertToExportSqlQuery(SqlQuery query)
    {
        return new SqlQueriesExportSqlQuery
        {
            Id = query.Id,
            Name = query.Name,
            Description = query.Description,
            Query = query.Query,
            ConnectionStringName = query.ConnectionStringName,
            Parameters = query.Parameters?.Select(p => new SqlQueriesExportSqlQueryParameter
            {
                Id = p.Id,
                Name = p.Name,
                Value = p.Value,
                Type = p.Type,
            }).ToList() ?? [],
        };
    }

    private static SqlQuery ConvertToSqlQuery(SqlQueriesExportSqlQuery exportSqlQuery)
    {
        var query = AbstractTypeFactory<SqlQuery>.TryCreateInstance();
        query.Id = exportSqlQuery.Id;
        query.Name = exportSqlQuery.Name;
        query.Description = exportSqlQuery.Description;
        query.Query = exportSqlQuery.Query;
        query.ConnectionStringName = exportSqlQuery.ConnectionStringName;
        query.Parameters = exportSqlQuery.Parameters?.Select(p =>
        {
            var parameter = AbstractTypeFactory<SqlQueryParameter>.TryCreateInstance();
            parameter.Id = p.Id;
            parameter.Name = p.Name;
            parameter.Value = p.Value;
            parameter.Type = p.Type;
            return parameter;
        }).ToList() ?? [];

        return query;
    }
}
