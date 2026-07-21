using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Maintenance;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IMaintenanceRequestService
{
    Task<PaginatedList<MaintenanceRequestViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        MaintenanceStatus? status,
        MaintenancePriority? priority,
        MaintenanceCategory? category,
        Guid? buildingId,
        Guid? vendorId,
        Guid? equipmentId,
        CancellationToken cancellationToken);

    Task<MaintenanceRequestViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task Create(MaintenanceRequestCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, MaintenanceRequestUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    Task UpdateStatus(Guid id, MaintenanceRequestStatusUpdateRequest request, CancellationToken cancellationToken);

    Task Assign(Guid id, MaintenanceRequestAssignRequest request, CancellationToken cancellationToken);

    Task<MaintenanceRequestStatsViewModel> GetStats(CancellationToken cancellationToken);
}
