using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Owner;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IOwnerService
{
    Task<PaginatedList<OwnerViewModel>> GetPaginated(int page, int pageSize, string? search, OwnerStatus? status, CancellationToken cancellationToken);
    Task<OwnerViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(OwnerCreateRequest request, CancellationToken cancellationToken);
    Task Update(OwnerUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}
