using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;
using VirtoCommerce.SqlQueries.Data.Models;
using VirtoCommerce.SqlQueries.Data.Repositories;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class SqlQuerySearchService : SearchService<SqlQuerySearchCriteria, SqlQuerySearchResult, SqlQuery, SqlQueryEntity>, ISqlQuerySearchService
{
    public SqlQuerySearchService(
        Func<ISqlQueriesRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        ISqlQueryService crudService,
        IOptions<CrudOptions> crudOptions)
        : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
    {
    }

    protected override IQueryable<SqlQueryEntity> BuildQuery(IRepository repository, SqlQuerySearchCriteria criteria)
    {
        var query = ((ISqlQueriesRepository)repository).SqlQueries;

        return query;
    }

    protected override IList<SortInfo> BuildSortExpression(SqlQuerySearchCriteria criteria)
    {
        var sortInfos = criteria.SortInfos;

        if (sortInfos.IsNullOrEmpty())
        {
            sortInfos =
            [
                new SortInfo
                {
                    SortColumn = nameof(SqlQuery.Name)
                }
            ];
        }

        return sortInfos;
    }
}
