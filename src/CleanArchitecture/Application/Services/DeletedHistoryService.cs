using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.DeletedHistory;

namespace CleanArchitecture.Application.Services;

public class DeletedHistoryService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : IDeletedHistoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<DeletedHistoryViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var histories = await _unitOfWork.DeletedHistoryRepository.GetAllAsync();
        var query = histories.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(x => x.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.DeletedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.DeletedAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x => x.EntityTitle.ToLower().Contains(cleanSearch) || x.EntityType.ToLower().Contains(cleanSearch));
        }

        query = query.OrderByDescending(x => x.DeletedAt);

        var totalCount = query.Count();
        var pagedItems = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Resolve user names for DeletedBy and RestoredBy
        var userIds = pagedItems.Select(x => x.DeletedBy)
            .Concat(pagedItems.Select(x => x.RestoredBy))
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var usersMap = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var u = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == userId);
            if (u != null)
            {
                usersMap[userId] = u.Name;
            }
        }

        var viewModels = pagedItems.Select(x => new DeletedHistoryViewModel
        {
            Id = x.Id,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            EntityTitle = x.EntityTitle,
            DeletedBy = x.DeletedBy,
            DeletedByName = x.DeletedBy.HasValue && usersMap.TryGetValue(x.DeletedBy.Value, out var name1) ? name1 : null,
            DeletedAt = x.DeletedAt,
            RestoredAt = x.RestoredAt,
            RestoredBy = x.RestoredBy,
            RestoredByName = x.RestoredBy.HasValue && usersMap.TryGetValue(x.RestoredBy.Value, out var name2) ? name2 : null
        }).ToList();

        return new PaginatedList<DeletedHistoryViewModel>(viewModels, totalCount, page, pageSize);
    }

    public async Task Restore(Guid id, CancellationToken cancellationToken)
    {
        throw DeletedHistoryException.BadRequestException("Restoring physically deleted records is not supported.");
    }

    public async Task DeletePermanently(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored and cannot be permanently deleted.");
        }

        _unitOfWork.DeletedHistoryRepository.Delete(history);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeletedHistoryDetailsViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        var deletedBy = history.DeletedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.DeletedBy.Value) : null;
        var restoredBy = history.RestoredBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.RestoredBy.Value) : null;

        return new DeletedHistoryDetailsViewModel
        {
            Id = history.Id,
            EntityType = history.EntityType,
            EntityId = history.EntityId,
            EntityTitle = history.EntityTitle,
            DeletedBy = history.DeletedBy,
            DeletedByName = deletedBy?.Name,
            DeletedAt = history.DeletedAt,
            RestoredAt = history.RestoredAt,
            RestoredBy = history.RestoredBy,
            RestoredByName = restoredBy?.Name,
            EntityData = null
        };
    }
}
