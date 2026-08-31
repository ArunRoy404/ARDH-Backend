using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Single source of truth for "what's the next occurrence date of a recurring maintenance
/// request". Uses calendar-correct AddMonths/AddYears for month-or-longer frequencies instead of
/// flat day-counts, so "Monthly" actually means one calendar month, not a drifting ~30 days.
/// </summary>
public static class MaintenanceRecurrenceHelper
{
    /// <summary>
    /// Returns the next occurrence date, or null when either input is missing.
    /// <paramref name="baseDate"/> should be the last completion date if one exists, otherwise the
    /// request's StartDate (i.e. the point in time the next interval is measured from).
    /// </summary>
    public static DateTime? GetNextOccurrence(DateTime? baseDate, MaintenanceRecurrenceFrequency? frequency)
    {
        if (!baseDate.HasValue || !frequency.HasValue)
        {
            return null;
        }

        return frequency.Value switch
        {
            MaintenanceRecurrenceFrequency.Daily => baseDate.Value.AddDays(1),
            MaintenanceRecurrenceFrequency.Weekly => baseDate.Value.AddDays(7),
            MaintenanceRecurrenceFrequency.BiWeekly => baseDate.Value.AddDays(14),
            MaintenanceRecurrenceFrequency.Monthly => baseDate.Value.AddMonths(1),
            MaintenanceRecurrenceFrequency.BiMonthly => baseDate.Value.AddMonths(2),
            MaintenanceRecurrenceFrequency.Quarterly => baseDate.Value.AddMonths(3),
            MaintenanceRecurrenceFrequency.HalfYearly => baseDate.Value.AddMonths(6),
            MaintenanceRecurrenceFrequency.Yearly => baseDate.Value.AddYears(1),
            MaintenanceRecurrenceFrequency.BiYearly => baseDate.Value.AddYears(2),
            _ => (DateTime?)null
        };
    }
}
