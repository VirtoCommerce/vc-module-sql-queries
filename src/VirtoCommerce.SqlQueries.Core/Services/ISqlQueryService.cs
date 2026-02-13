using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Services;

public interface ISqlQueryService : ICrudService<SqlQuery>
{
    Task<SqlQueryReport> GenerateReport(SqlQuery query, IList<SqlQueryParameter> parameters, string format, SqlQueryReportContext context);

    Task<SqlQueryExecuteResult> ExecuteQuery(SqlQueryExecuteRequest request);

    IList<string> GetFormats();

    DatabaseInformation GetDatabaseInformation();
}
