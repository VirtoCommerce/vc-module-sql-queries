using System.Data;
using System.Text;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Data.Services;
using Xunit;

namespace VirtoCommerce.SqlQueries.Tests;

[Trait("Category", "Unit")]
public class HtmlSqlQueryReportGeneratorTests
{
    [Fact]
    public void GenerateReport_MarksNumericColumnsForRightAlignment()
    {
        var table = new DataTable();
        table.Columns.Add("StoreId", typeof(string));
        table.Columns.Add("TotalOrders", typeof(int));
        table.Columns.Add("Total", typeof(decimal));
        table.Rows.Add("B2B-store", 67, 59384m);

        var html = Generate(table);

        Assert.Contains("<th>Store<wbr>Id</th>", html);
        Assert.Contains("<th class=\"num\">Total<wbr>Orders</th>", html);
        Assert.Contains("<td>B2B-store</td>", html);
        Assert.Contains("<td class=\"num\">67</td>", html);
        Assert.Contains("<td class=\"num\">59384</td>", html);
    }

    [Theory]
    [InlineData("AQAAAAIAAYagAAAAEJ3q7Yt0x0Zk6p1o9m3Qf8sV2b4n6c8d0e2f4g", true)]
    [InlineData("https://example.com/assets/images/products/large/item.png", true)]
    [InlineData("c43afe1f-15b8-421c-a1c0-5ef1ca41b4bd", false)]
    [InlineData("A plain sentence with several ordinary words in it", false)]
    public void GenerateReport_LetsLongWordsBreakAnywhere(string value, bool expected)
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(string));
        table.Rows.Add(value);

        var html = Generate(table);

        Assert.Equal(expected, html.Contains("<td class=\"long-word\">"));
    }

    [Fact]
    public void GenerateReport_EncodesColumnNamesAndValues()
    {
        var table = new DataTable();
        table.Columns.Add("Name<script>", typeof(string));
        table.Rows.Add("<b>bold</b>");

        var html = Generate(table);

        Assert.Contains("<th>Name&lt;script&gt;</th>", html);
        Assert.Contains("<td>&lt;b&gt;bold&lt;/b&gt;</td>", html);
    }

    private static string Generate(DataTable table)
    {
        var report = new HtmlSqlQueryReportGenerator().GenerateReport(table, new SqlQueryReportContext { Name = "Test" });
        return Encoding.UTF8.GetString(report.Content);
    }
}
