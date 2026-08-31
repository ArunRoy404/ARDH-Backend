using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Apartment;
using CleanArchitecture.Shared.Models.ApartmentChargeHistory;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IApartmentService
{
    Task<PaginatedList<ApartmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        string? apartmentType,
        string? status,
        CancellationToken cancellationToken);

    Task<ApartmentViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task<byte[]> ExportToXlsx(
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        string? apartmentType,
        string? status,
        CancellationToken cancellationToken);

    Task Create(ApartmentCreateRequest request, CancellationToken cancellationToken);
    Task Update(Guid id, ApartmentUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<List<ApartmentChargeHistoryViewModel>> GetChargeHistory(Guid apartmentId, CancellationToken cancellationToken);
}
