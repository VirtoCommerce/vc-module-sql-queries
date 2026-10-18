using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public partial class HtmlSqlQueryReportGenerator : IHtmlSqlQueryReportGenerator
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
        var isNumeric = new bool[table.Columns.Count];

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");

        for (var i = 0; i < table.Columns.Count; i++)
        {
            isNumeric[i] = IsNumericColumn(table.Columns[i]);
            sb.AppendLine($"<th{GetClassAttribute(isNumeric[i], longWord: false)}>{FormatColumnName(table.Columns[i].ColumnName)}</th>");
        }

        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine("<tr>");

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var item = row[i];
                var value = item is DBNull ? "" : item?.ToString() ?? "";
                sb.AppendLine($"<td{GetClassAttribute(isNumeric[i], HasLongWord(value))}>{HttpUtility.HtmlEncode(value)}</td>");
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

    protected virtual bool IsNumericColumn(DataColumn column)
    {
        return Type.GetTypeCode(Nullable.GetUnderlyingType(column.DataType) ?? column.DataType) is >= TypeCode.SByte and <= TypeCode.Decimal;
    }

    private static string GetClassAttribute(bool numeric, bool longWord)
    {
        return (numeric, longWord) switch
        {
            (true, true) => " class=\"num long-word\"",
            (true, false) => " class=\"num\"",
            (false, true) => " class=\"long-word\"",
            _ => "",
        };
    }

    /// <summary>
    /// Lets a column name such as LastPasswordChangedDate wrap between its words in print;
    /// on screen the header stays on one line.
    /// </summary>
    protected virtual string FormatColumnName(string columnName)
    {
        return WordBoundaryRegex().Replace(HttpUtility.HtmlEncode(columnName), "<wbr>");
    }

    /// <summary>
    /// A value with a word longer than this (a hash, token, or URL) may break anywhere in print,
    /// otherwise its column alone would set the minimum width of the whole table.
    /// </summary>
    protected virtual int LongWordLength => 30;

    protected virtual bool HasLongWord(string value)
    {
        var wordLength = 0;

        foreach (var c in value)
        {
            wordLength = char.IsWhiteSpace(c) || c == '-' ? 0 : wordLength + 1;

            if (wordLength > LongWordLength)
            {
                return true;
            }
        }

        return false;
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
            th.num, td.num {
                text-align: right;
                font-variant-numeric: tabular-nums;
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
            @page {
                size: landscape;
                margin: 12mm 10mm;
                @bottom-right {
                    content: "Page " counter(page) " of " counter(pages);
                    font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                    font-size: 7pt;
                    color: #5a6b85;
                }
            }
            @media print {
                body {
                    padding: 0;
                    font-size: 9pt;
                    color: #1f2937;
                    -webkit-print-color-adjust: exact;
                    print-color-adjust: exact;
                }
                .report-header {
                    margin-bottom: 8px;
                    padding-bottom: 6px;
                    border-bottom-width: 1.5px;
                    break-after: avoid;
                }
                .report-header h1 { font-size: 14pt; }
                .report-description { font-size: 9pt; margin-bottom: 2px; }
                .report-meta { font-size: 8pt; color: #5a6b85; }
                table {
                    width: 100%;
                    font-size: 7.5pt;
                    line-height: 1.3;
                }
                /* Repeat the column headers on every page */
                thead { display: table-header-group; }
                /* Dark text on a rule, not white on a fill: browsers drop backgrounds unless "Background graphics" is on */
                thead th {
                    background: none;
                    color: #374767;
                    padding: 4px 5px;
                    border-bottom: 1.5px solid #374767;
                    vertical-align: bottom;
                    white-space: normal;
                    max-width: none;
                }
                /* Break long values only when they overflow, so a narrow column never collapses to one character per line */
                tbody td {
                    padding: 3px 5px;
                    border-bottom: 0.5pt solid #d5dde6;
                    max-width: none;
                    word-break: normal;
                    overflow-wrap: break-word;
                }
                tbody td.long-word {
                    overflow-wrap: anywhere;
                    min-width: 12em;
                }
                tbody tr:nth-child(even) { background: #f6f8fa; }
                tbody tr:hover { background: inherit; }
                tbody tr:nth-child(even):hover { background: #f6f8fa; }
                /* Keep a row on one page */
                tr { break-inside: avoid; }
                .report-footer {
                    margin-top: 8px;
                    padding-top: 4px;
                    font-size: 7pt;
                    break-before: avoid;
                }
            }
            """;
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=_)(?=.)")]
    private static partial Regex WordBoundaryRegex();
}
