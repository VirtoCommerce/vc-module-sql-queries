using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.SqlQueries.Data.Models;

namespace VirtoCommerce.SqlQueries.Data.Repositories;
public class SqlQueriesRepository : DbContextRepositoryBase<SqlQueriesDbContext>, ISqlQueriesRepository
{
    public SqlQueriesRepository(SqlQueriesDbContext dbContext)
        : base(dbContext)
    {
    }

    public IQueryable<SqlQueryEntity> SqlQueries => DbContext.Set<SqlQueryEntity>();

    public IQueryable<SqlQueryParameterEntity> SqlQueryParameters => DbContext.Set<SqlQueryParameterEntity>();

    public async Task<IList<SqlQueryEntity>> GetSqlQueriesByIdsAsync(IList<string> ids)
    {
        var sqlQueries = await SqlQueries
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

        if (sqlQueries.Count != 0)
        {
            var sqlQueryIds = sqlQueries.Select(x => x.Id).ToList();
            // Ensure that the parameters are loaded for each query
            await SqlQueryParameters.Where(x => ids.Contains(x.SqlQueryId)).LoadAsync();
        }

        return sqlQueries;
    }
}
