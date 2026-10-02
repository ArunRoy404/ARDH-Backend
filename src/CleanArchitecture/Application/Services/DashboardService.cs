using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Dashboard;

namespace CleanArchitecture.Application.Services;

public class DashboardService(IUnitOfWork unitOfWork) : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Normalizes an optional start/end date pair to date-only (time stripped) and
    /// rejects an inverted range. A null value on either side means "unbounded" on
    /// that side - no range at all (both null) means "all time".
    /// </summary>
    private static (DateTime? Start, DateTime? End) ResolveRange(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate?.Date;
        var end = endDate?.Date;

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw DashboardException.BadRequestException("startDate must not be later than endDate.");
        }

        return (start, end);
    }

    public async Task<DashboardStatsViewModel> GetStats(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        // 1. Buildings count
        var totalBuildings = hasBuildingFilter
            ? (await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == buildingId!.Value) ? 1 : 0)
            : await _unitOfWork.BuildingRepository.CountAsync();

        // 2. Apartments count
        var totalApartments = await _unitOfWork.ApartmentRepository.CountAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);

        // 3. Occupancy (current snapshot - always "right now", not date-ranged)
        var occupied = await _unitOfWork.ApartmentRepository.CountAsync(a =>
            (!hasBuildingFilter || a.BuildingId == buildingId!.Value) &&
            a.CurrentTenantId != null && a.CurrentTenantId != Guid.Empty);
        var vacant = Math.Max(0, totalApartments - occupied);

        // 4. Pending Payments (current actionable count, not date-ranged)
        var pendingPaymentsCount = await _unitOfWork.IncomeRecordRepository.CountAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (x.Status == IncomeStatus.Pending || x.Status == IncomeStatus.Overdue));

        // 5. Open Maintenance Requests (current actionable count, not date-ranged)
        var openMaintenanceCount = await _unitOfWork.MaintenanceRequestRepository.CountAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress));

        return new DashboardStatsViewModel
        {
            TotalBuildings = totalBuildings,
            TotalApartments = totalApartments,
            OccupiedCount = occupied,
            VacantCount = vacant,
            PendingPaymentsCount = pendingPaymentsCount,
            OpenMaintenanceCount = openMaintenanceCount
        };
    }

    public async Task<DashboardAmountStatViewModel> GetIncomeStat(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;
        var (start, end) = ResolveRange(startDate, endDate);

        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            x.Status == IncomeStatus.Paid &&
            (!start.HasValue || x.PaymentDate.Date >= start.Value) &&
            (!end.HasValue || x.PaymentDate.Date <= end.Value));

        return new DashboardAmountStatViewModel { Amount = records.Sum(x => x.Amount) };
    }

    public async Task<DashboardAmountStatViewModel> GetExpenseStat(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;
        var (start, end) = ResolveRange(startDate, endDate);

        var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            x.Status == ExpenseStatus.Paid &&
            (!start.HasValue || x.ExpenseDate.Date >= start.Value) &&
            (!end.HasValue || x.ExpenseDate.Date <= end.Value));

        return new DashboardAmountStatViewModel { Amount = records.Sum(x => x.Amount) };
    }

    public async Task<OccupancyOverviewViewModel> GetOccupancy(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;
        var (start, end) = ResolveRange(startDate, endDate);
        var now = DateTime.UtcNow;

        // Occupied/Reserved always come from lease overlap with a window - no
        // params defaults the window to "just today", so the default case is
        // just a special case of the same formula, not a separate code path.
        var windowStart = start ?? now.Date;
        var windowEnd = end ?? now.Date;

        var totalApartments = await _unitOfWork.ApartmentRepository.CountAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);

        var overlappingTenants = await _unitOfWork.TenantRepository.GetAllAsync(t =>
            (!hasBuildingFilter || t.BuildingId == buildingId!.Value) &&
            t.Status == TenantStatus.Active &&
            (t.LeaseEndDate == null || t.LeaseEndDate >= windowStart) &&
            t.LeaseStartDate <= windowEnd);

        var occupied = overlappingTenants.Where(t => t.LeaseStartDate <= now).Select(t => t.ApartmentId).Distinct().Count();
        var reserved = overlappingTenants.Count(t => t.LeaseStartDate > now);

        // Maintenance is always the current open/in-progress backlog, regardless
        // of the selected window - an open issue stays relevant whether or not
        // it was filed inside the range being looked at.
        var maintenanceCount = await _unitOfWork.MaintenanceRequestRepository.CountAsync(m =>
            (!hasBuildingFilter || m.BuildingId == buildingId!.Value) &&
            m.ApartmentId != null &&
            (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress));

        var vacant = Math.Max(0, totalApartments - occupied - maintenanceCount - reserved);

        return new OccupancyOverviewViewModel
        {
            Occupied = occupied,
            Vacant = vacant,
            Maintenance = maintenanceCount,
            Reserved = reserved,
            Total = totalApartments
        };
    }

    public async Task<List<ExpenseBreakdownItemViewModel>> GetExpenseBreakdown(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;
        var (start, end) = ResolveRange(startDate, endDate);

        var expenseRecords = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            x.Status == ExpenseStatus.Paid &&
            (!start.HasValue || x.ExpenseDate.Date >= start.Value) &&
            (!end.HasValue || x.ExpenseDate.Date <= end.Value));

        var breakdown = expenseRecords
            .GroupBy(x => x.Category)
            .Select(g => new ExpenseBreakdownItemViewModel
            {
                Category = g.Key.ToString(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return breakdown;
    }

    public async Task<PaginatedList<DashboardRecentPaymentViewModel>> GetRecentPayments(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var totalItems = await _unitOfWork.IncomeRecordRepository.CountAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var pageRecords = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var items = pageRecords
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DashboardRecentPaymentViewModel
            {
                FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var flat) ? flat : null,
                BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
                IncomeType = x.IncomeType.ToString(),
                PaymentDate = x.PaymentDate,
                Amount = x.Amount,
                Status = x.Status.ToString()
            }).ToList();

        return new PaginatedList<DashboardRecentPaymentViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedList<DashboardOpenMaintenanceViewModel>> GetOpenMaintenance(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var totalItems = await _unitOfWork.MaintenanceRequestRepository.CountAsync(x => 
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress));

        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => 
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress));

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var items = requests
            .OrderBy(x => x.Priority == MaintenancePriority.High ? 1 : x.Priority == MaintenancePriority.Medium ? 2 : 3)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => {
                var bName = x.BuildingId != Guid.Empty && buildingMap.TryGetValue(x.BuildingId, out var b) ? b : "Unknown Building";
                var flat = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var f) ? f : "Common Area";
                return new DashboardOpenMaintenanceViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Location = $"{bName} • {flat}",
                    Priority = x.Priority.ToString(),
                    Status = x.Status.ToString()
                };
            }).ToList();

        return new PaginatedList<DashboardOpenMaintenanceViewModel>(items, totalItems, page, pageSize);
    }
}
