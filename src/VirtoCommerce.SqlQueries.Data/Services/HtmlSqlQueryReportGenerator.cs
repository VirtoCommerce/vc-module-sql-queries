using System;
using System.Data;
using System.Text;
using System.Web;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class HtmlSqlQueryReportGenerator : IHtmlSqlQueryReportGenerator
{
    public string Format => "html";
    public string ContentType => "text/html";
    public int Priority => 40;

    public virtual SqlQueryReport GenerateReport(DataTable table, SqlQueryReportContext context)
    {
        var sb = new StringBuilder();
        var title = HttpUtility.HtmlEncode(context?.Name ?? "SQL Query Report");

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetReportStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("<div class=\"report-header\">");
        sb.AppendLine($"<h1>{title}</h1>");

        if (!string.IsNullOrWhiteSpace(context?.Description))
        {
            sb.AppendLine($"<div class=\"report-description\">{HttpUtility.HtmlEncode(context.Description)}</div>");
        }

        sb.Append($"<div class=\"report-meta\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");

        if (!string.IsNullOrWhiteSpace(context?.UserName))
        {
            sb.Append($" &middot; By: {HttpUtility.HtmlEncode(context.UserName)}");
        }

        sb.AppendLine($" &middot; {table.Rows.Count:N0} rows &middot; {table.Columns.Count} columns</div>");
        sb.AppendLine("</div>");

        // Table
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");

        foreach (DataColumn column in table.Columns)
        {
            sb.AppendLine($"<th>{HttpUtility.HtmlEncode(column.ColumnName)}</th>");
        }

        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine("<tr>");

            foreach (var item in row.ItemArray)
            {
                var value = item is DBNull ? "" : item?.ToString() ?? "";
                sb.AppendLine($"<td>{HttpUtility.HtmlEncode(value)}</td>");
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        // Footer
        sb.AppendLine("<div class=\"report-footer\">");
        sb.AppendLine($"Report generated on {DateTime.UtcNow:yyyy-MM-dd} at {DateTime.UtcNow:HH:mm:ss} UTC");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var report = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        report.Content = Encoding.UTF8.GetBytes(sb.ToString());
        report.ContentType = ContentType;

        return report;
    }

    protected virtual string GetReportStyles()
    {
        return """
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
                font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                font-size: 13px;
                color: #333;
                padding: 24px;
                background: #fff;
            }
            .report-header {
                margin-bottom: 20px;
                padding-bottom: 12px;
                border-bottom: 2px solid #374767;
            }
            .report-header h1 {
                font-size: 20px;
                font-weight: 600;
                color: #374767;
                margin-bottom: 4px;
            }
            .report-description {
                font-size: 13px;
                color: #555;
                margin-bottom: 6px;
            }
            .report-meta {
                font-size: 12px;
                color: #8094ae;
            }
            table {
                border-collapse: collapse;
                font-size: 12px;
                table-layout: auto;
            }
            thead th {
                background: #374767;
                color: #fff;
                font-weight: 600;
                text-align: left;
                padding: 8px 10px;
                white-space: nowrap;
                max-width: 450px;
            }
            tbody td {
                padding: 6px 10px;
                border-bottom: 1px solid #e8eef3;
                vertical-align: top;
                max-width: 450px;
                word-break: break-word;
            }
            tbody tr:nth-child(even) {
                background: #f8fafc;
            }
            tbody tr:hover {
                background: #e6f7ff;
            }
            .report-footer {
                margin-top: 20px;
                padding-top: 10px;
                border-top: 1px solid #e8eef3;
                font-size: 11px;
                color: #8094ae;
                text-align: right;
            }
            @media print {
                body { padding: 0; }
                tbody tr:hover { background: inherit; }
            }
            """;
    }
}
