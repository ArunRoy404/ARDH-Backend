using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Occupancy;

namespace CleanArchitecture.Application.Services;

public class OccupancyReportService(IUnitOfWork unitOfWork) : IOccupancyReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PaginatedList<OccupancyReportItemViewModel>> GetOccupancyReport(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var allItems = await BuildReportItems(buildingId, fromDate, toDate);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var pageItems = allItems
            .OrderBy(x => x.BuildingName)
            .ThenBy(x => x.FlatNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<OccupancyReportItemViewModel>(pageItems, allItems.Count, page, pageSize);
    }

    public async Task<OccupancyReportSummaryViewModel> GetOccupancyReportSummary(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var items = await BuildReportItems(buildingId, fromDate, toDate);

        return new OccupancyReportSummaryViewModel
        {
            ApartmentCount = items.Count,
            TotalReportDays = items.Count > 0 ? items[0].TotalReportDays : 0,
            TotalOccupiedDays = items.Sum(x => x.OccupiedDays),
            TotalVacantDays = items.Sum(x => x.VacantDays),
            TotalExpectedOccupiedRent = items.Sum(x => x.ExpectedOccupiedRent),
            TotalVacancyRentValue = items.Sum(x => x.VacancyRentValue),
            TotalPotentialRent = items.Sum(x => x.TotalPotentialRent)
        };
    }

    public async Task<byte[]> ExportOccupancyReportToXlsx(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var items = await BuildReportItems(buildingId, fromDate, toDate);

        var rows = new List<List<string>>
        {
            new() { "BuildingName", "FlatNumber", "TotalReportDays", "OccupiedDays", "VacantDays", "OccupiedDuration", "VacantDuration", "ExpectedOccupiedRent", "VacancyRentValue", "TotalPotentialRent" }
        };

        foreach (var item in items.OrderBy(x => x.BuildingName).ThenBy(x => x.FlatNumber))
        {
            rows.Add(new List<string>
            {
                item.BuildingName,
                item.FlatNumber,
                item.TotalReportDays.ToString(),
                item.OccupiedDays.ToString(),
                item.VacantDays.ToString(),
                item.OccupiedDurationDisplay,
                item.VacantDurationDisplay,
                item.ExpectedOccupiedRent.ToString("F2"),
                item.VacancyRentValue.ToString("F2"),
                item.TotalPotentialRent.ToString("F2")
            });
        }

        return XlsxHelper.BuildXlsx(rows, "Occupancy");
    }

    private async Task<List<OccupancyReportItemViewModel>> BuildReportItems(Guid? buildingId, DateTime fromDate, DateTime toDate)
    {
        if (fromDate == default || toDate == default)
        {
            throw ReportException.BadRequestException("fromDate and toDate are required.");
        }

        if (fromDate.Date > toDate.Date)
        {
            throw ReportException.BadRequestException("The report's from-date cannot be after its to-date.");
        }

        var rangeStart = fromDate.Date;
        var clampedTo = OccupancyCalculationHelper.ClampToToday(toDate);

        if (rangeStart > clampedTo)
        {
            throw ReportException.BadRequestException("The report's from-date cannot be after today - occupancy cannot be calculated for the future.");
        }

        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x =>
            !hasBuildingFilter || x.BuildingId == buildingId!.Value);

        if (apartments.Count == 0)
        {
            return new List<OccupancyReportItemViewModel>();
        }

        var apartmentIds = apartments.Select(a => a.Id).ToList();
        var buildingIds = apartments.Select(a => a.BuildingId).Distinct().ToList();

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => buildingIds.Contains(x.Id));
        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);

        // Full lease history per apartment (not just what overlaps the window) - needed to tell
        // a vacancy gap that sits between two known leases apart from one with no anchoring lease
        // at all on one side.
        var allLeases = await _unitOfWork.TenantRepository.GetAllAsync(x => apartmentIds.Contains(x.ApartmentId));
        var leasesByApartment = allLeases
            .GroupBy(t => t.ApartmentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.LeaseStartDate).ToList());

        var tenantIds = allLeases.Select(t => t.Id).ToList();
        var rentHistory = tenantIds.Count > 0
            ? await _unitOfWork.TenantRentHistoryRepository.GetAllAsync(x => tenantIds.Contains(x.TenantId))
            : new List<TenantRentHistory>();
        var rentHistoryByTenant = rentHistory
            .GroupBy(h => h.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.EffectiveFrom).ToList());

        var totalReportDays = OccupancyCalculationHelper.InclusiveDayCount(rangeStart, clampedTo);
        var results = new List<OccupancyReportItemViewModel>();

        foreach (var apartment in apartments)
        {
            var leases = leasesByApartment.TryGetValue(apartment.Id, out var l) ? l : new List<Tenant>();

            var occupiedIntervals = new List<(DateTime Start, DateTime End, Tenant Lease)>();
            foreach (var lease in leases)
            {
                var clip = OccupancyCalculationHelper.ClipToRange(lease.LeaseStartDate, lease.LeaseEndDate, rangeStart, clampedTo);
                if (clip.HasValue)
                {
                    occupiedIntervals.Add((clip.Value.Start, clip.Value.End, lease));
                }
            }

            var (trimmedOccupied, gaps) = BuildTimeline(occupiedIntervals, rangeStart, clampedTo);
            var vacantDays = gaps.Sum(g => OccupancyCalculationHelper.InclusiveDayCount(g.Start, g.End));
            var occupiedDays = totalReportDays - vacantDays;

            var expectedOccupiedRent = trimmedOccupied.Sum(x => ComputeOccupiedRent(x.Lease, x.Start, x.End, rentHistoryByTenant));
            var vacancyRentValue = gaps.Sum(g => ComputeVacancyRent(g, leases, apartment, rentHistoryByTenant));

            var roundedExpectedOccupiedRent = Math.Round(expectedOccupiedRent, 2, MidpointRounding.AwayFromZero);
            var roundedVacancyRentValue = Math.Round(vacancyRentValue, 2, MidpointRounding.AwayFromZero);

            var (occupiedMonths, occupiedRemDays) = OccupancyCalculationHelper.GetCalendarDurationFromDayCount(rangeStart, occupiedDays);
            var (vacantMonths, vacantRemDays) = OccupancyCalculationHelper.GetCalendarDurationFromDayCount(rangeStart, vacantDays);

            results.Add(new OccupancyReportItemViewModel
            {
                ApartmentId = apartment.Id,
                FlatNumber = apartment.FlatNumber,
                BuildingId = apartment.BuildingId,
                BuildingName = buildingMap.TryGetValue(apartment.BuildingId, out var bName) ? bName : "Unknown Building",
                TotalReportDays = totalReportDays,
                OccupiedDays = occupiedDays,
                VacantDays = vacantDays,
                OccupiedDurationDisplay = FormatDuration(occupiedMonths, occupiedRemDays),
                VacantDurationDisplay = FormatDuration(vacantMonths, vacantRemDays),
                ExpectedOccupiedRent = roundedExpectedOccupiedRent,
                VacancyRentValue = roundedVacancyRentValue,
                TotalPotentialRent = roundedExpectedOccupiedRent + roundedVacancyRentValue
            });
        }

        return results;
    }

    /// <summary>
    /// Walks the range left-to-right against the occupied intervals once, producing both the
    /// leftover gaps AND the occupied intervals trimmed so they never overlap each other. Doing
    /// both in the same pass guarantees OccupiedDays/VacantDays and the rent figures computed from
    /// these same trimmed intervals always agree - even if two leases' own dates happen to overlap
    /// (pre-existing legacy data, or a lease whose end date was never correctly closed out), the
    /// earlier-starting lease claims the days first and the later one only counts what's left.
    /// </summary>
    private static (List<(DateTime Start, DateTime End, Tenant Lease)> Occupied, List<(DateTime Start, DateTime End)> Gaps) BuildTimeline(
        List<(DateTime Start, DateTime End, Tenant Lease)> occupiedIntervals,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var occupied = new List<(DateTime, DateTime, Tenant)>();
        var gaps = new List<(DateTime, DateTime)>();
        var cursor = rangeStart;

        foreach (var interval in occupiedIntervals.OrderBy(x => x.Start))
        {
            if (interval.Start > cursor)
            {
                var gapEnd = interval.Start.AddDays(-1);
                if (gapEnd >= cursor)
                {
                    gaps.Add((cursor, gapEnd));
                }
            }

            var effectiveStart = interval.Start > cursor ? interval.Start : cursor;
            if (effectiveStart <= interval.End)
            {
                occupied.Add((effectiveStart, interval.End, interval.Lease));
            }

            if (interval.End >= cursor)
            {
                cursor = interval.End.AddDays(1);
            }
        }

        if (cursor <= rangeEnd)
        {
            gaps.Add((cursor, rangeEnd));
        }

        return (occupied, gaps);
    }

    /// <summary>
    /// Walks the lease's rent-history segments left to right across the occupied interval,
    /// prorating each. Any stretch of the interval not covered by a segment - e.g. the lease's
    /// start date was corrected to be earlier after the segment was recorded - falls back to the
    /// tenant's current flat rent rather than silently contributing zero for those days.
    /// </summary>
    private static decimal ComputeOccupiedRent(
        Tenant lease,
        DateTime intervalStart,
        DateTime intervalEnd,
        Dictionary<Guid, List<TenantRentHistory>> rentHistoryByTenant)
    {
        if (!rentHistoryByTenant.TryGetValue(lease.Id, out var segments) || segments.Count == 0)
        {
            return OccupancyCalculationHelper.ProrateRent(lease.MonthlyRent, intervalStart, intervalEnd);
        }

        decimal total = 0m;
        var cursor = intervalStart;

        foreach (var segment in segments)
        {
            var segStart = segment.EffectiveFrom.Date;
            var segEnd = (segment.EffectiveTo ?? intervalEnd).Date;

            var clipStart = segStart > cursor ? segStart : cursor;
            var clipEnd = segEnd < intervalEnd ? segEnd : intervalEnd;

            if (clipStart > clipEnd)
            {
                continue;
            }

            if (clipStart > cursor)
            {
                total += OccupancyCalculationHelper.ProrateRent(lease.MonthlyRent, cursor, clipStart.AddDays(-1));
            }

            total += OccupancyCalculationHelper.ProrateRent(segment.MonthlyRent, clipStart, clipEnd);
            cursor = clipEnd.AddDays(1);
        }

        if (cursor <= intervalEnd)
        {
            total += OccupancyCalculationHelper.ProrateRent(lease.MonthlyRent, cursor, intervalEnd);
        }

        return total;
    }

    /// <summary>
    /// A gap sitting between two known leases (a lease ended before it, another starts after it -
    /// anywhere in the apartment's full history, not just inside the report window) uses the
    /// preceding lease's final rent, matching the spec's own worked example (a vacant month
    /// between two tenancies is valued at the outgoing tenant's rate). A gap with no lease on one
    /// side at all - before the apartment's first-ever tenant, or an open-ended vacancy after the
    /// last one with nothing lined up next - uses the apartment's current asking rent instead.
    /// </summary>
    private static decimal ComputeVacancyRent(
        (DateTime Start, DateTime End) gap,
        List<Tenant> leases,
        Apartment apartment,
        Dictionary<Guid, List<TenantRentHistory>> rentHistoryByTenant)
    {
        var precedingLease = leases
            .Where(t => t.LeaseEndDate.HasValue && t.LeaseEndDate.Value.Date < gap.Start)
            .OrderByDescending(t => t.LeaseEndDate)
            .FirstOrDefault();

        var followingLeaseExists = leases.Any(t => t.LeaseStartDate.Date > gap.End);

        if (precedingLease != null && followingLeaseExists)
        {
            var precedingRent = rentHistoryByTenant.TryGetValue(precedingLease.Id, out var history) && history.Count > 0
                ? history.OrderByDescending(h => h.EffectiveFrom).First().MonthlyRent
                : precedingLease.MonthlyRent;

            return OccupancyCalculationHelper.ProrateRent(precedingRent, gap.Start, gap.End);
        }

        return OccupancyCalculationHelper.ProrateRent(apartment.ExpectedRent, gap.Start, gap.End);
    }

    private static string FormatDuration(int months, int days)
    {
        var monthLabel = months == 1 ? "month" : "months";
        var dayLabel = days == 1 ? "day" : "days";
        return $"{months} {monthLabel}, {days} {dayLabel}";
    }
}
