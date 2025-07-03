using System;
using System.Data;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;
using Document = iText.Layout.Document;
using Table = iText.Layout.Element.Table;

namespace VirtoCommerce.SqlQueries.Data.Services;
public class PdfSqlQueryReportGenerator() : ISqlQueryReportGenerator
{
    public string Format => "pdf";
    public string ContentType => "application/pdf";

    public SqlQueryReport GenerateReport(DataTable table)
    {
        using var memoryStream = new MemoryStream();
        var writer = new PdfWriter(memoryStream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        document.Add(new Paragraph($"SQL Query Report").SetFontSize(10));
        document.Add(new Paragraph($"Generated: {DateTime.Now}").SetFontSize(10));

        var pdfTable = new Table(table.Columns.Count);

        // Add header row
        foreach (DataColumn column in table.Columns)
        {
            pdfTable.AddHeaderCell(column.ColumnName);
        }

        // Add data rows
        foreach (DataRow row in table.Rows)
        {
            foreach (var item in row.ItemArray)
            {
                pdfTable.AddCell(item?.ToString() ?? string.Empty);
            }
        }

        document.Add(pdfTable);
        document.Close();

        var report = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        report.Content = memoryStream.ToArray();
        report.ContentType = ContentType;

        return report;
    }
}
