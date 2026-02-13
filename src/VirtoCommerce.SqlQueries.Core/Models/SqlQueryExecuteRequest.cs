using System.Collections.Generic;

namespace VirtoCommerce.SqlQueries.Core.Models;

public class SqlQueryExecuteRequest
{
    public string Query { get; set; }

    public string ConnectionStringName { get; set; }

    public IList<SqlQueryParameter> Parameters { get; set; } = [];

    public int MaxRows { get; set; } = 100;
}
