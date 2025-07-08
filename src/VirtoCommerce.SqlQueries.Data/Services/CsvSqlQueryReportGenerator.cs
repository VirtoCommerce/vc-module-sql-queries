using System.Data;
using System.Linq;
using System.Text;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class CsvSqlQueryReportGenerator : ISqlQueryReportGenerator
{
    public string Format => "csv";
    public string ContentType => "text/csv";

    public SqlQueryReport GenerateReport(DataTable table)
    {
        var stringBuilder = new StringBuilder();

        // Header
        var columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        stringBuilder.AppendLine(string.Join(",", columnNames));

        // Rows
        foreach (DataRow row in table.Rows)
        {
            var fields = row.ItemArray.Select(field => "\"" + (field?.ToString().Replace("\"", "\"\"") ?? "") + "\"");
            stringBuilder.AppendLine(string.Join(",", fields));
        }

        var report = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        report.Content = Encoding.UTF8.GetBytes(stringBuilder.ToString());
        report.ContentType = ContentType;

        return report;
    }
}
