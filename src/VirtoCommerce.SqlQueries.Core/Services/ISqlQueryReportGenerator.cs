using System.Data;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Services;

public interface ISqlQueryReportGenerator
{
    string Format { get; }
    string ContentType { get; }

    /// <summary>
    /// Display/order priority. Higher value means higher priority (listed first).
    /// </summary>
    int Priority => 0;

    SqlQueryReport GenerateReport(DataTable table, SqlQueryReportContext context);
}
