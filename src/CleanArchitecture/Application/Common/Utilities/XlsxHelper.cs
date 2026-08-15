using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Minimal XLSX (.xlsx) helper used by the bulk upload and module export features.
/// <para>
/// Reading converts every cell to its plain string form (numbers invariant, dates as
/// <c>yyyy-MM-dd</c>, booleans as <c>true</c>/<c>false</c>) so the existing row-based
/// parsers keep working unchanged. Writing emits one worksheet with a bold, frozen
/// header row and auto-sized columns.
/// </para>
/// </summary>
public static class XlsxHelper
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>MIME type for .xlsx files, shared by controllers that serve them.</summary>
    public static string ContentType => XlsxContentType;

    /// <summary>
    /// Reads the first worksheet of an XLSX stream into rows of plain strings.
    /// Empty cells are padded so every row has the same column count as the header.
    /// Returns an empty list when the sheet has no used range.
    /// </summary>
    public static List<List<string>> ReadRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<List<string>>();
        var range = worksheet.RangeUsed();
        if (range == null)
        {
            return rows;
        }

        var firstRow = range.FirstRow().RowNumber();
        var lastRow = range.LastRow().RowNumber();
        var firstCol = range.FirstColumn().ColumnNumber();
        var lastCol = range.LastColumn().ColumnNumber();

        for (var r = firstRow; r <= lastRow; r++)
        {
            var rowValues = new List<string>();
            for (var c = firstCol; c <= lastCol; c++)
            {
                rowValues.Add(GetCellText(worksheet.Cell(r, c)));
            }
            rows.Add(rowValues);
        }

        return rows;
    }

    /// <summary>
    /// Serializes rows to an XLSX byte array with a bold, frozen header row and
    /// auto-sized columns. The first row is treated as the header.
    /// </summary>
    public static byte[] BuildXlsx(List<List<string>> rows, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var r = 0; r < rows.Count; r++)
        {
            var rowValues = rows[r];
            for (var c = 0; c < rowValues.Count; c++)
            {
                worksheet.Cell(r + 1, c + 1).Value = rowValues[c];
            }
        }

        if (rows.Count > 0)
        {
            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.SheetView.FreezeRows(1);
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string GetCellText(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsBlank)
        {
            return string.Empty;
        }

        if (value.IsText)
        {
            return value.GetText();
        }

        if (value.IsNumber)
        {
            return value.GetNumber().ToString(CultureInfo.InvariantCulture);
        }

        if (value.IsDateTime)
        {
            var dateTime = value.GetDateTime();
            return dateTime.TimeOfDay == TimeSpan.Zero
                ? dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (value.IsBoolean)
        {
            return value.GetBoolean() ? "true" : "false";
        }

        if (value.IsTimeSpan)
        {
            return value.GetTimeSpan().ToString();
        }

        return value.ToString() ?? string.Empty;
    }
}
