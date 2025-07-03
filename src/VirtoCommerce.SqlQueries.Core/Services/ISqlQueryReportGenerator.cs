using System.Data;
using VirtoCommerce.SqlQueries.Core.Models;

namespace VirtoCommerce.SqlQueries.Core.Services;
public interface ISqlQueryReportGenerator
{
    string Format { get; }
    string ContentType { get; }

    SqlQueryReport GenerateReport(DataTable table);
}
