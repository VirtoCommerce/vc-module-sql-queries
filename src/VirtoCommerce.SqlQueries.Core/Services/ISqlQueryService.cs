using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Services;
public interface ISqlQueryService : ICrudService<SqlQuery>
{
    Task<SqlQueryReport> GenerateReport(SqlQuery query, string format);

    IList<string> GetFormats();

    IList<string> GetAvailableConnectionStrings();
}
