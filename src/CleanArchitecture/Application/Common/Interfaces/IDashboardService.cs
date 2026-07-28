using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models.Dashboard;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsViewModel> GetStats(Guid? buildingId, CancellationToken cancellationToken);
    Task<OccupancyOverviewViewModel> GetOccupancy(Guid? buildingId, CancellationToken cancellationToken);
    Task<List<ExpenseBreakdownItemViewModel>> GetExpenseBreakdown(Guid? buildingId, CancellationToken cancellationToken);
    Task<List<DashboardRecentPaymentViewModel>> GetRecentPayments(Guid? buildingId, CancellationToken cancellationToken);
    Task<List<DashboardOpenMaintenanceViewModel>> GetOpenMaintenance(Guid? buildingId, CancellationToken cancellationToken);
}
