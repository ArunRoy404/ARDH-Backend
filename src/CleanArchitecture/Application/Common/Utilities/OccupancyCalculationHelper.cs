using System;
using System.Collections.Generic;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Date-interval math shared by the occupancy report: clamping the report window to today,
/// clipping a lease's own date range against that window, and splitting any inclusive date
/// range into calendar-month chunks - used both to prorate rent (days-in-chunk / days-in-month)
/// and to render calendar-accurate "N months, N days" figures instead of totalDays / 30.
/// </summary>
public static class OccupancyCalculationHelper
{
    public static DateTime ClampToToday(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        return date.Date > today ? today : date.Date;
    }

    public static int InclusiveDayCount(DateTime startInclusive, DateTime endInclusive)
        => (endInclusive.Date - startInclusive.Date).Days + 1;

    /// <summary>
    /// Clips a lease's own <paramref name="periodStart"/>/<paramref name="periodEnd"/> (null end =
    /// still ongoing) against the report window. Returns null when there is no overlap.
    /// </summary>
    public static (DateTime Start, DateTime End)? ClipToRange(DateTime periodStart, DateTime? periodEnd, DateTime rangeStart, DateTime rangeEnd)
    {
        var effectiveEnd = periodEnd?.Date ?? rangeEnd.Date;
        var clippedStart = periodStart.Date > rangeStart.Date ? periodStart.Date : rangeStart.Date;
        var clippedEnd = effectiveEnd < rangeEnd.Date ? effectiveEnd : rangeEnd.Date;

        return clippedStart > clippedEnd ? null : (clippedStart, clippedEnd);
    }

    /// <summary>
    /// Splits an inclusive [start, end] date range into per-calendar-month chunks.
    /// </summary>
    public static IEnumerable<(DateTime ChunkStart, DateTime ChunkEnd, int DaysInChunk, int DaysInMonth)> SplitByCalendarMonth(DateTime startInclusive, DateTime endInclusive)
    {
        var cursor = startInclusive.Date;
        var exclusiveEnd = endInclusive.Date.AddDays(1);

        while (cursor < exclusiveEnd)
        {
            var monthStart = new DateTime(cursor.Year, cursor.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var monthExclusiveEnd = monthStart.AddMonths(1);
            var chunkExclusiveEnd = monthExclusiveEnd < exclusiveEnd ? monthExclusiveEnd : exclusiveEnd;

            var chunkStart = cursor;
            var chunkEnd = chunkExclusiveEnd.AddDays(-1);
            var daysInChunk = (chunkExclusiveEnd - chunkStart).Days;

            yield return (chunkStart, chunkEnd, daysInChunk, daysInMonth);

            cursor = chunkExclusiveEnd;
        }
    }

    /// <summary>
    /// Prorates a monthly rent across an inclusive date range: MonthlyRent x ApplicableDays / DaysInThatMonth,
    /// summed per calendar month so a rate that only applied for part of a month is only charged for that part.
    /// </summary>
    public static decimal ProrateRent(decimal monthlyRent, DateTime startInclusive, DateTime endInclusive)
    {
        decimal total = 0m;
        foreach (var chunk in SplitByCalendarMonth(startInclusive, endInclusive))
        {
            total += monthlyRent * chunk.DaysInChunk / chunk.DaysInMonth;
        }
        return total;
    }

    /// <summary>
    /// Converts a (possibly non-contiguous, e.g. summed across several leases) day count into a
    /// calendar-accurate (months, days) figure by walking real month lengths from <paramref name="anchor"/> -
    /// still not totalDays / 30. When the underlying days are actually contiguous and start at
    /// <paramref name="anchor"/>, this is exact; otherwise it's a calendar-correct duration figure
    /// for display, not a literal disjoint span.
    /// </summary>
    public static (int Months, int Days) GetCalendarDurationFromDayCount(DateTime anchor, int totalDays)
    {
        return totalDays <= 0 ? (0, 0) : GetCalendarMonthsAndDays(anchor.Date, anchor.Date.AddDays(totalDays - 1));
    }

    /// <summary>
    /// Calendar-accurate (months, days) for display - actual whole calendar months plus the
    /// remainder, not totalDays / 30 (e.g. a full non-leap year is "12 months, 0 days").
    /// </summary>
    public static (int Months, int Days) GetCalendarMonthsAndDays(DateTime startInclusive, DateTime endInclusive)
    {
        if (endInclusive.Date < startInclusive.Date)
        {
            return (0, 0);
        }

        var exclusiveEnd = endInclusive.Date.AddDays(1);
        var cursor = startInclusive.Date;
        var months = 0;

        while (cursor.AddMonths(1) <= exclusiveEnd)
        {
            cursor = cursor.AddMonths(1);
            months++;
        }

        var days = (exclusiveEnd - cursor).Days;
        return (months, days);
    }
}
