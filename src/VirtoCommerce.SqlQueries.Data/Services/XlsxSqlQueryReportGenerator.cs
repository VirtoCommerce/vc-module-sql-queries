using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class XlsxSqlQueryReportGenerator : ISqlQueryReportGenerator
{
    protected const string DateFormat = "dd.MM.yyyy";
    protected const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";

    public string Format => "xlsx";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public int Priority => 20;

    public virtual SqlQueryReport GenerateReport(DataTable table, SqlQueryReportContext context)
    {
        using var memoryStream = new MemoryStream();

        MiniExcel.SaveAs(memoryStream, table, excelType: ExcelType.XLSX, configuration: CreateConfiguration(table));

        var result = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        result.Content = memoryStream.ToArray();
        result.ContentType = ContentType;

        return result;
    }

    protected virtual OpenXmlConfiguration CreateConfiguration(DataTable table)
    {
        return new OpenXmlConfiguration
        {
            // Write binary columns as text instead of embedding them into the workbook as files or images
            EnableConvertByteArray = false,
            TrimColumnNames = false,
            DynamicColumns = GetDynamicColumns(table).ToArray(),
        };
    }

    protected virtual IEnumerable<DynamicExcelColumn> GetDynamicColumns(DataTable table)
    {
        // MiniExcel matches dynamic columns by name case-insensitively and fails on duplicates
        var columns = table.Columns
            .Cast<DataColumn>()
            .DistinctBy(x => x.Caption, StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            var format = GetColumnFormat(column);

            if (format != null)
            {
                yield return new DynamicExcelColumn(column.Caption) { Format = format };
            }
        }
    }

    protected virtual string GetColumnFormat(DataColumn column)
    {
        var type = column.DataType;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return DateTimeFormat;
        }

        if (type == typeof(DateOnly))
        {
            return DateFormat;
        }

        return null;
    }
}
