using System.Collections.Generic;

namespace VirtoCommerce.SqlQueries.Core.Models;

public class SqlQueryExecuteByIdRequest
{
    public IList<SqlQueryParameter> Parameters { get; set; } = [];

    public int MaxRows { get; set; } = 100;
}
