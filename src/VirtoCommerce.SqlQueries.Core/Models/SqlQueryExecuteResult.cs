using System.Collections.Generic;

namespace VirtoCommerce.SqlQueries.Core.Models;

public class SqlQueryExecuteResult
{
    public IList<SqlQueryExecuteColumn> Columns { get; set; } = [];

    public IList<object[]> Rows { get; set; } = [];

    public int TotalRowCount { get; set; }

    public bool IsTruncated { get; set; }

    public long ExecutionTimeMs { get; set; }
}
