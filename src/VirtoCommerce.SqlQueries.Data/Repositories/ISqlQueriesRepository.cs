using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Data.Models;

namespace VirtoCommerce.SqlQueries.Data.Repositories;
public interface ISqlQueriesRepository : IRepository
{
    public IQueryable<SqlQueryEntity> SqlQueries { get; }

    Task<IList<SqlQueryEntity>> GetSqlQueriesByIdsAsync(IList<string> ids);
}
