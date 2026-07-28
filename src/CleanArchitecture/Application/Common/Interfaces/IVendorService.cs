using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Vendor;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IVendorService
{
    Task<PaginatedList<VendorViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        VendorType? vendorType,
        VendorStatus? status,
        CancellationToken cancellationToken);

    Task<VendorViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task Create(VendorCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, VendorUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);
}
