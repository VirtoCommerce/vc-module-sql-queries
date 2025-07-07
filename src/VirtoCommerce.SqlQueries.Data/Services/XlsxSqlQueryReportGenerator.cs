using System;
using System.Data;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SqlQueries.Core.Models;
using VirtoCommerce.SqlQueries.Core.Services;

namespace VirtoCommerce.SqlQueries.Data.Services;

public class XlsxSqlQueryReportGenerator() : ISqlQueryReportGenerator
{
    protected const string dateFormat = "dd.MM.yyyy";
    protected const string dateTimeFormat = "dd.MM.yyyy HH:mm.ss";

    public string Format => "xlsx";
    public string ContentType => "application/vnd.ms-excel";

    public SqlQueryReport GenerateReport(DataTable table)
    {
        var workbook = new XSSFWorkbook();

        var sheet = workbook.CreateSheet("Sheet1");

        var headerRow = sheet.CreateRow(0);
        for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
        {
            var headerCell = headerRow.CreateCell(colIndex);
            SetCellValue(headerCell, table.Columns[colIndex].ColumnName);
        }

        for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = sheet.CreateRow(rowIndex + 1);

            for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
            {
                var cell = row.CreateCell(colIndex);
                SetCellValue(cell, table.Rows[rowIndex].ItemArray.GetValue(colIndex));
            }
        }

        using var memoryStream = new MemoryStream();

        workbook.Write(memoryStream);

        var result = AbstractTypeFactory<SqlQueryReport>.TryCreateInstance();
        result.Content = memoryStream.ToArray();
        result.ContentType = ContentType;

        return result;
    }

    protected virtual void SetCellValue(ICell cell, object dataValue)
    {
        if (dataValue == null)
        {
            return;
        }

        if (dataValue is string)
        {
            cell.SetCellValue((string)dataValue);
        }
        else if (IsNumber(dataValue))
        {
            cell.SetCellValue(Convert.ToDouble(dataValue));
        }
        else if (dataValue is bool)
        {
            cell.SetCellValue((bool)dataValue);
        }
        else if (dataValue is DateTime)
        {
            SetCellDateFormat(cell, dateFormat);
            cell.SetCellValue((DateTime)dataValue);
        }
        else if (dataValue is DateOnly)
        {
            SetCellDateFormat(cell, dateTimeFormat);
            cell.SetCellValue((DateOnly)dataValue);
        }
        else
        {
            cell.SetCellValue(dataValue?.ToString());
        }
    }

    protected void SetCellDateFormat(ICell cell, string format)
    {
        var dateStyle = cell.Sheet.Workbook.CreateCellStyle();
        var dateFormat = cell.Sheet.Workbook.CreateDataFormat();
        dateStyle.DataFormat = dateFormat.GetFormat(format);
        cell.CellStyle = dateStyle;
    }

    protected bool IsNumber(object value)
    {
        return value is double
            || value is decimal
            || value is int
            || value is short
            || value is long
            || value is float
            || value is byte;
    }
}
