using System.Collections.Generic;

namespace VirtoCommerce.SqlQueries.Data.ExportImport;

public class SqlQueriesExportSqlQuery
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Query { get; set; }
    public string ConnectionStringName { get; set; }
    public IList<SqlQueriesExportSqlQueryParameter> Parameters { get; set; } = [];
}

public class SqlQueriesExportSqlQueryParameter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public object Value { get; set; }
    public string Type { get; set; }
}
