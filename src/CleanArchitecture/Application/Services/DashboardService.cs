using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Dashboard;

namespace CleanArchitecture.Application.Services;

public class DashboardService(IUnitOfWork unitOfWork) : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<DashboardStatsViewModel> GetStats(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        // 1. Buildings count
        var totalBuildings = hasBuildingFilter 
            ? (await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == buildingId!.Value) ? 1 : 0)
            : await _unitOfWork.BuildingRepository.CountAsync();

        // 2. Apartments
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var totalApartments = apartments.Count;

        // 3. Active tenants & maintenance requests for occupancy logic
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var activeTenants = tenants.Where(t => t.Status == TenantStatus.Active).ToList();

        var maintenanceRequests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var activeMaintenance = maintenanceRequests.Where(m => m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress).ToList();

        var occupied = 0;
        var vacant = 0;

        foreach (var a in apartments)
        {
            var isOccupied = (a.CurrentTenantId != null && a.CurrentTenantId != Guid.Empty) 
                || activeTenants.Any(t => t.ApartmentId == a.Id && t.LeaseStartDate <= DateTime.UtcNow);

            if (isOccupied)
            {
                occupied++;
            }
            else
            {
                vacant++;
            }
        }

        // 4. Financial metrics for the current calendar month
        var now = DateTime.UtcNow;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        var incomeRecords = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var expenseRecords = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);

        var monthlyIncome = incomeRecords
            .Where(x => x.Status == IncomeStatus.Paid && x.PaymentDate.Month == currentMonth && x.PaymentDate.Year == currentYear)
            .Sum(x => x.Amount);

        var monthlyExpense = expenseRecords
            .Where(x => x.Status == ExpenseStatus.Paid && x.ExpenseDate.Month == currentMonth && x.ExpenseDate.Year == currentYear)
            .Sum(x => x.Amount);

        // 5. Pending Payments
        var pendingPaymentsCount = incomeRecords
            .Count(x => x.Status == IncomeStatus.Pending || x.Status == IncomeStatus.Overdue);

        // 6. Open Maintenance Requests
        var openMaintenanceCount = activeMaintenance.Count;

        return new DashboardStatsViewModel
        {
            TotalBuildings = totalBuildings,
            TotalApartments = totalApartments,
            OccupiedCount = occupied,
            VacantCount = vacant,
            MonthlyIncome = monthlyIncome,
            MonthlyExpense = monthlyExpense,
            PendingPaymentsCount = pendingPaymentsCount,
            OpenMaintenanceCount = openMaintenanceCount
        };
    }

    public async Task<OccupancyOverviewViewModel> GetOccupancy(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var activeTenants = tenants.Where(t => t.Status == TenantStatus.Active).ToList();

        var maintenanceRequests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var activeMaintenance = maintenanceRequests.Where(m => m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress).ToList();

        var occupied = 0;
        var vacant = 0;
        var maintenanceCount = 0;
        var reserved = 0;

        foreach (var a in apartments)
        {
            var isOccupied = (a.CurrentTenantId != null && a.CurrentTenantId != Guid.Empty) 
                || activeTenants.Any(t => t.ApartmentId == a.Id && t.LeaseStartDate <= DateTime.UtcNow);

            if (isOccupied)
            {
                occupied++;
            }
            else if (activeTenants.Any(t => t.ApartmentId == a.Id && t.LeaseStartDate > DateTime.UtcNow))
            {
                reserved++;
            }
            else if (activeMaintenance.Any(m => m.ApartmentId == a.Id))
            {
                maintenanceCount++;
            }
            else
            {
                vacant++;
            }
        }

        return new OccupancyOverviewViewModel
        {
            Occupied = occupied,
            Vacant = vacant,
            Maintenance = maintenanceCount,
            Reserved = reserved,
            Total = apartments.Count
        };
    }

    public async Task<List<ExpenseBreakdownItemViewModel>> GetExpenseBreakdown(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var expenseRecords = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => 
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            x.Status == ExpenseStatus.Paid);

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

    public async Task<List<DashboardRecentPaymentViewModel>> GetRecentPayments(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => !hasBuildingFilter || x.BuildingId == buildingId!.Value);
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync();
        var tenantMap = tenants.ToDictionary(t => t.Id, t => t.FullName);

        var recent = records
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new DashboardRecentPaymentViewModel
            {
                TenantName = x.TenantId.HasValue && tenantMap.TryGetValue(x.TenantId.Value, out var name) ? name : "Unknown Tenant",
                IncomeType = x.IncomeType.ToString(),
                PaymentDate = x.PaymentDate,
                Amount = x.Amount,
                Status = x.Status.ToString()
            })
            .ToList();

        return recent;
    }

    public async Task<List<DashboardOpenMaintenanceViewModel>> GetOpenMaintenance(Guid? buildingId, CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => 
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress));

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var recent = requests
            .OrderBy(x => x.Priority == MaintenancePriority.High ? 1 : x.Priority == MaintenancePriority.Medium ? 2 : 3)
            .ThenByDescending(x => x.CreatedAt)
            .Take(10)
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
            })
            .ToList();

        return recent;
    }
}
