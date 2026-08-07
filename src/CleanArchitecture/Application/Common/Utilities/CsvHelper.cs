using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Minimal, dependency-free CSV parser/writer.
/// Handles quoted fields, escaped quotes (""), commas and newlines inside quotes,
/// and provides case-insensitive, whitespace-tolerant header lookup.
/// </summary>
public static class CsvHelper
{
    /// <summary>
    /// Parses CSV text into rows of raw cell values. The first row is treated as the header.
    /// Cell values are returned without surrounding quotes ("" inside quotes are unescaped).
    /// </summary>
    public static List<List<string>> Parse(string csvText)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return rows;
        }

        // Strip UTF-8 BOM if present
        if (csvText.Length > 0 && csvText[0] == '\uFEFF')
        {
            csvText = csvText.Substring(1);
        }

        var currentRow = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        bool fieldStarted = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        fieldStarted = true;
                        break;
                    case ',':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        fieldStarted = false;
                        break;
                    case '\r':
                        // Ignore CR (handled with LF below)
                        break;
                    case '\n':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        fieldStarted = false;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    default:
                        currentField.Append(c);
                        fieldStarted = true;
                        break;
                }
            }
        }

        // Flush trailing field/row (no trailing newline)
        if (fieldStarted || currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }

    /// <summary>
    /// Builds a normalized lookup dictionary from a header row.
    /// Keys are case-insensitive, trimmed, and stripped of underscores/spaces
    /// so "BuildingId", "building_id", "Building ID" and "buildingid" all match.
    /// </summary>
    public static Dictionary<string, int> BuildHeaderIndex(List<string> header)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var key = NormalizeHeader(header[i]);
            if (!string.IsNullOrEmpty(key) && !index.ContainsKey(key))
            {
                index[key] = i;
            }
        }
        return index;
    }

    /// <summary>Normalizes a header name: lowercase, trim, remove spaces/underscores/dashes.</summary>
    public static string NormalizeHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }
        return new string(header
            .Where(ch => !char.IsWhiteSpace(ch) && ch != '_' && ch != '-')
            .ToArray())
            .ToLowerInvariant();
    }

    /// <summary>
    /// Extracts a cell value by header name (case/format insensitive). Returns null when
    /// the header is missing or the value is empty/whitespace.
    /// </summary>
    public static string? GetValue(List<string> row, Dictionary<string, int> headerIndex, string headerName)
    {
        var key = NormalizeHeader(headerName);
        if (!headerIndex.TryGetValue(key, out var idx) || idx >= row.Count)
        {
            return null;
        }
        var value = row[idx]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>Serializes rows (List of fields) to CSV text with proper quoting.</summary>
    public static string BuildCsv(IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            var fields = row.Select(EscapeField);
            sb.AppendLine(string.Join(",", fields));
        }
        return sb.ToString();
    }

    private static string EscapeField(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
