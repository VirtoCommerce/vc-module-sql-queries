using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Services;
public interface ISqlQuerySearchService : ISearchService<SqlQuerySearchCriteria, SqlQuerySearchResult, SqlQuery>
{
}
