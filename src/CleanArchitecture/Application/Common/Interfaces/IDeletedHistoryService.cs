using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.DeletedHistory;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IDeletedHistoryService
{
    Task<PaginatedList<DeletedHistoryViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);

    Task Restore(Guid id, CancellationToken cancellationToken);

    Task DeletePermanently(Guid id, CancellationToken cancellationToken);

    Task<DeletedHistoryDetailsViewModel> GetById(Guid id, CancellationToken cancellationToken);
}
