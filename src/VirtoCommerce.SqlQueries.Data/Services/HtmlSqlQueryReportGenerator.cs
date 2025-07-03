using System;
using System.Data;
using System.Text;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;
public class HtmlSqlQueryReportGenerator : ISqlQueryReportGenerator
{
    public string Format => "html";
    public string ContentType => "text/html";

    public SqlQueryReport GenerateReport(DataTable table)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("<html><body>");
        stringBuilder.AppendLine("<h1>SQL Query Report</h1>");
        stringBuilder.AppendLine($"<p>Generated: {DateTime.Now}</p>");
        stringBuilder.AppendLine("<table border='1'>");

        // Add header row
        stringBuilder.AppendLine("<tr>");
        foreach (DataColumn column in table.Columns)
        {
            stringBuilder.AppendLine($"<th>{column.ColumnName}</th>");
        }
        stringBuilder.AppendLine("</tr>");

        // Add data rows
        foreach (DataRow row in table.Rows)
        {
            stringBuilder.AppendLine("<tr>");
            foreach (var item in row.ItemArray)
            {
                stringBuilder.AppendLine($"<td>{item?.ToString() ?? string.Empty}</td>");
            }
            stringBuilder.AppendLine("</tr>");
        }

        stringBuilder.AppendLine("</table>");
        stringBuilder.AppendLine("</body></html>");

        var report = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        report.Content = Encoding.UTF8.GetBytes(stringBuilder.ToString());
        report.ContentType = ContentType;

        return report;
    }
}
