using System;
using System.Globalization;

namespace CleanArchitecture.Application.Common.Utilities;

public static class TimeOfDeliveryHelper
{
    /// <summary>
    /// Parses a "time of delivery" value that may be a bare time (e.g. "13:31", "01:31 PM") or a
    /// full ISO date-time. A bare time is combined with the date portion of <paramref name="referenceDate"/>
    /// (falling back to today's UTC date if not supplied).
    /// </summary>
    public static bool TryParse(string? raw, DateTime? referenceDate, out DateTime? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (TimeOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            var datePart = (referenceDate ?? DateTime.UtcNow).Date;
            result = DateTime.SpecifyKind(datePart.Add(time.ToTimeSpan()), DateTimeKind.Utc);
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var full))
        {
            result = full;
            return true;
        }

        return false;
    }
}
