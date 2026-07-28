using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Tenant;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ITenantService
{
    Task<PaginatedList<TenantViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? apartmentId,
        TenantStatus? status,
        CancellationToken cancellationToken);

    Task<TenantViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(TenantCreateRequest request, CancellationToken cancellationToken);
    Task Update(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}
