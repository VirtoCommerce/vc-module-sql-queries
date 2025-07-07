using System.Data;
using System.Text;
using DinkToPdf;
using DinkToPdf.Contracts;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class PdfSqlQueryReportGenerator(IHtmlSqlQueryReportGenerator htmlGenerator, IConverter converter) : ISqlQueryReportGenerator
{
    public string Format => "pdf";
    public string ContentType => "application/pdf";

    public SqlQueryReport GenerateReport(DataTable table)
    {
        var htmlEncoding = Encoding.UTF8;

        var htmlGenerationResult = htmlGenerator.GenerateReport(table);
        var html = htmlEncoding.GetString(htmlGenerationResult.Content);

        var doc = new HtmlToPdfDocument();
        doc.GlobalSettings.PaperSize = PaperKind.A4;
        doc.Objects.Add(new ObjectSettings()
        {
            HtmlContent = html,
            WebSettings =
            {
                DefaultEncoding = htmlEncoding.WebName
            }
        });

        var pdfBytes = converter.Convert(doc);

        var result = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        result.Content = pdfBytes;
        result.ContentType = ContentType;

        return result;
    }
}
