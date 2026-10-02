using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Dashboard;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsViewModel> GetStats(Guid? buildingId, CancellationToken cancellationToken);
    Task<DashboardAmountStatViewModel> GetIncomeStat(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);
    Task<DashboardAmountStatViewModel> GetExpenseStat(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);
    Task<OccupancyOverviewViewModel> GetOccupancy(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);
    Task<List<ExpenseBreakdownItemViewModel>> GetExpenseBreakdown(Guid? buildingId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);
    Task<PaginatedList<DashboardRecentPaymentViewModel>> GetRecentPayments(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PaginatedList<DashboardOpenMaintenanceViewModel>> GetOpenMaintenance(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken);
}
