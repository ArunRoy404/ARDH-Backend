using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IApartmentService
{
    Task<PaginatedList<ApartmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        ApartmentType? apartmentType,
        CancellationToken cancellationToken);

    Task<ApartmentViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(ApartmentCreateRequest request, CancellationToken cancellationToken);
    Task Update(Guid id, ApartmentUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}
