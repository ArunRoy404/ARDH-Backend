using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Equipment;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IEquipmentService
{
    Task<PaginatedList<EquipmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        string? type,
        string? status,
        CancellationToken cancellationToken);

    Task<EquipmentViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task<byte[]> ExportToCsv(
        string? search,
        Guid? buildingId,
        string? type,
        string? status,
        CancellationToken cancellationToken);

    Task Create(EquipmentCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, EquipmentUpdateRequest request, CancellationToken cancellationToken);

    Task UpdateStatus(Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);
}
