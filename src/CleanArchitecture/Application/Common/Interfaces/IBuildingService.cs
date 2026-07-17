using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Building;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IBuildingService
{
    Task<PaginatedList<BuildingViewModel>> GetPaginated(int page, int pageSize, string? search, BuildingStatus? status, CancellationToken cancellationToken);
    Task<BuildingViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(BuildingCreateRequest request, CancellationToken cancellationToken);
    Task Update(BuildingUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}
