using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Header lookup helpers shared by the bulk upload row parsers.
/// Headers are matched case-insensitively, trimmed, and stripped of
/// underscores/spaces/dashes so "BuildingName", "building_name" and
/// "building name" all resolve to the same column.
/// </summary>
public static class CsvHelper
{
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
}
