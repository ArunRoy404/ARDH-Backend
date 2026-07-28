using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models.Tenant;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ITenantMoveOutService
{
    Task CreateMoveOut(Guid tenantId, TenantMoveOutCreateRequest request, CancellationToken cancellationToken);
    Task<TenantMoveOutRecordViewModel> GetByTenantId(Guid tenantId, CancellationToken cancellationToken);
    Task UpdateMoveOut(Guid tenantId, TenantMoveOutUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteMoveOut(Guid tenantId, CancellationToken cancellationToken);
}
