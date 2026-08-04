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
    Task<OccupancyOverviewViewModel> GetOccupancy(Guid? buildingId, CancellationToken cancellationToken);
    Task<List<ExpenseBreakdownItemViewModel>> GetExpenseBreakdown(Guid? buildingId, CancellationToken cancellationToken);
    Task<PaginatedList<DashboardRecentPaymentViewModel>> GetRecentPayments(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PaginatedList<DashboardOpenMaintenanceViewModel>> GetOpenMaintenance(Guid? buildingId, int page, int pageSize, CancellationToken cancellationToken);
}
