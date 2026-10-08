using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Data.Services;
using Xunit;

namespace VirtoCommerce.SqlQueries.Tests;

[Trait("Category", "Unit")]
public class XlsxSqlQueryReportGeneratorTests
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [Fact]
    public void GenerateReport_ReturnsOpenXmlContentType()
    {
        var generator = new XlsxSqlQueryReportGenerator();

        var report = generator.GenerateReport(new DataTable(), new SqlQueryReportContext());

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", generator.ContentType);
        Assert.Equal(generator.ContentType, report.ContentType);
    }

    [Fact]
    public void GenerateReport_WritesHeaderAndTypedValues()
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("Price", typeof(decimal));
        table.Columns.Add("IsActive", typeof(bool));
        table.Columns.Add("Note", typeof(string));
        table.Rows.Add("Widget", 3, 12.34m, true, DBNull.Value);

        var workbook = Generate(table);

        Assert.Equal(["Name", "Quantity", "Price", "IsActive", "Note"], workbook.GetRowTexts(1));
        Assert.Equal("Widget", workbook.GetText("A2"));
        Assert.Equal("3", workbook.GetNumber("B2"));
        Assert.Equal("12.34", workbook.GetNumber("C2"));
        Assert.Equal("b", workbook.GetCell("D2").Attribute("t")?.Value);
        Assert.Equal("1", workbook.GetCell("D2").Element(Ns + "v")?.Value);
        Assert.Equal(string.Empty, workbook.GetText("E2"));
    }

    [Fact]
    public void GenerateReport_DateTimeColumn_UsesDateTimeFormat()
    {
        var table = new DataTable();
        table.Columns.Add("CreatedDate", typeof(DateTime));
        table.Rows.Add(new DateTime(2026, 2, 3, 14, 15, 16));

        var workbook = Generate(table);

        Assert.Equal(new DateTime(2026, 2, 3, 14, 15, 16).ToOADate().ToString(CultureInfo.InvariantCulture), workbook.GetNumber("A2"));
        Assert.Equal("dd.MM.yyyy HH:mm:ss", workbook.GetNumberFormat("A2"));
    }

    [Fact]
    public void GenerateReport_DateOnlyColumn_UsesDateFormat()
    {
        var table = new DataTable();
        table.Columns.Add("BirthDate", typeof(DateOnly));
        table.Rows.Add(new DateOnly(2026, 2, 3));

        var workbook = Generate(table);

        Assert.Equal(new DateTime(2026, 2, 3).ToOADate().ToString(CultureInfo.InvariantCulture), workbook.GetNumber("A2"));
        Assert.Equal("dd.MM.yyyy", workbook.GetNumberFormat("A2"));
    }

    [Fact]
    public void GenerateReport_ManyDateCells_ReusesCellStyles()
    {
        var table = new DataTable();
        table.Columns.Add("CreatedDate", typeof(DateTime));
        table.Columns.Add("ModifiedDate", typeof(DateTime));
        for (var i = 0; i < 1000; i++)
        {
            table.Rows.Add(DateTime.Today.AddDays(i), DateTime.Today.AddDays(-i));
        }

        var workbook = Generate(table);

        Assert.True(workbook.CellStyleCount < 50, $"Expected a small fixed set of cell styles, got {workbook.CellStyleCount}");
        Assert.Equal("dd.MM.yyyy HH:mm:ss", workbook.GetNumberFormat("B1001"));
    }

    [Fact]
    public void GenerateReport_ByteArrayColumn_IsNotEmbeddedAsFile()
    {
        var table = new DataTable();
        table.Columns.Add("Payload", typeof(byte[]));
        table.Rows.Add(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var workbook = Generate(table);

        Assert.DoesNotContain(workbook.EntryNames, x => x.StartsWith("xl/media/", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateReport_ColumnNamesDifferingByCase_DoesNotThrow()
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("date", typeof(DateTime));
        table.Rows.Add(DateTime.Today, DateTime.Today);

        var workbook = Generate(table);

        Assert.Equal(["Date", "date"], workbook.GetRowTexts(1));
    }

    private static XlsxWorkbook Generate(DataTable table)
    {
        var report = new XlsxSqlQueryReportGenerator().GenerateReport(table, new SqlQueryReportContext());

        return new XlsxWorkbook(report.Content);
    }

    private sealed class XlsxWorkbook
    {
        private readonly Dictionary<string, XElement> _cells;
        private readonly List<string> _sharedStrings;
        private readonly List<XElement> _cellFormats;
        private readonly Dictionary<int, string> _numberFormats;

        public XlsxWorkbook(byte[] content)
        {
            using var archive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);

            EntryNames = archive.Entries.Select(x => x.FullName).ToList();

            var sheet = Load(archive, "xl/worksheets/sheet1.xml");
            _cells = sheet.Descendants(Ns + "c").ToDictionary(x => x.Attribute("r")!.Value);

            var sharedStrings = Load(archive, "xl/sharedStrings.xml");
            _sharedStrings = sharedStrings?.Root!.Elements(Ns + "si").Select(x => string.Concat(x.Descendants(Ns + "t").Select(t => t.Value))).ToList() ?? [];

            var styles = Load(archive, "xl/styles.xml");
            _cellFormats = styles.Root!.Element(Ns + "cellXfs")!.Elements(Ns + "xf").ToList();
            _numberFormats = styles.Root.Element(Ns + "numFmts")?.Elements(Ns + "numFmt")
                .ToDictionary(x => int.Parse(x.Attribute("numFmtId")!.Value), x => x.Attribute("formatCode")!.Value) ?? [];
        }

        public IList<string> EntryNames { get; }

        public int CellStyleCount => _cellFormats.Count;

        public XElement GetCell(string reference)
        {
            return _cells[reference];
        }

        public IList<string> GetRowTexts(int row)
        {
            return _cells.Keys
                .Where(x => x.TrimStart('A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z') == row.ToString(CultureInfo.InvariantCulture))
                .Select(GetText)
                .ToList();
        }

        public string GetText(string reference)
        {
            var cell = _cells[reference];
            var type = cell.Attribute("t")?.Value;

            return type switch
            {
                "s" => _sharedStrings[int.Parse(cell.Element(Ns + "v")!.Value)],
                "inlineStr" => string.Concat(cell.Descendants(Ns + "t").Select(x => x.Value)),
                _ => cell.Element(Ns + "v")?.Value ?? string.Empty,
            };
        }

        public string GetNumber(string reference)
        {
            var cell = _cells[reference];
            var type = cell.Attribute("t")?.Value;

            Assert.True(type is null or "n", $"Cell {reference} is expected to be numeric but has type '{type}'");

            return cell.Element(Ns + "v")!.Value;
        }

        public string GetNumberFormat(string reference)
        {
            var styleIndex = int.Parse(_cells[reference].Attribute("s")?.Value ?? "0");
            var numberFormatId = int.Parse(_cellFormats[styleIndex].Attribute("numFmtId")?.Value ?? "0");

            return _numberFormats.GetValueOrDefault(numberFormatId);
        }

        private static XDocument Load(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name);
            if (entry == null)
            {
                return null;
            }

            using var stream = entry.Open();

            return XDocument.Load(stream);
        }
    }
}
