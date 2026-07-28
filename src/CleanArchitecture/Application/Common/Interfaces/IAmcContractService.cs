using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.AmcContract;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IAmcContractService
{
    Task<PaginatedList<AmcContractViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        AmcStatus? status,
        AmcContractType? contractType,
        Guid? vendorId,
        Guid? equipmentId,
        CancellationToken cancellationToken);

    Task<AmcContractViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task<AmcContractStatsViewModel> GetStats(CancellationToken cancellationToken);

    Task Create(AmcContractCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, AmcContractUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);
}
