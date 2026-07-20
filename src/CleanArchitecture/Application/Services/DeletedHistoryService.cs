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
using CleanArchitecture.Shared.Models.User;
using CleanArchitecture.Shared.Models.Building;

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
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored.");
        }

        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original User not found.");
            
            user.DeletedAt = null;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserRepository.Update(user);
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Building not found.");
            
            building.DeletedAt = null;
            building.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BuildingRepository.Update(building);
        }
        else if (history.EntityType.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Owner not found.");
            
            owner.DeletedAt = null;
            owner.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.OwnerRepository.Update(owner);
        }
        else if (history.EntityType.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Apartment not found.");
            
            apartment.DeletedAt = null;
            apartment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ApartmentRepository.Update(apartment);
        }
        else
        {
            throw DeletedHistoryException.BadRequestException($"Unknown entity type: {history.EntityType}");
        }

        history.RestoredAt = DateTime.UtcNow;
        history.RestoredBy = _currentUser.GetCurrentUserId();
        _unitOfWork.DeletedHistoryRepository.Update(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePermanently(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored and cannot be permanently deleted.");
        }

        // Delete underlying entity permanently
        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (user != null)
            {
                _unitOfWork.UserRepository.Delete(user);
            }
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (building != null)
            {
                _unitOfWork.BuildingRepository.Delete(building);
            }
        }
        else if (history.EntityType.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (owner != null)
            {
                _unitOfWork.OwnerRepository.Delete(owner);
            }
        }
        else if (history.EntityType.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (apartment != null)
            {
                _unitOfWork.ApartmentRepository.Delete(apartment);
            }
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

        object? entityData = null;
        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (user != null)
            {
                entityData = new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Address = user.Address ?? string.Empty,
                    Role = user.Role,
                    AvatarUrl = user.AvatarUrl ?? string.Empty,
                    IsActive = user.IsActive,
                    Permissions = user.Permissions ?? string.Empty,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };
            }
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (building != null)
            {
                entityData = new BuildingViewModel
                {
                    Id = building.Id,
                    BuildingName = building.BuildingName,
                    Address = building.Address,
                    City = building.City,
                    State = building.State,
                    Country = building.Country,
                    GoogleMapLink = building.GoogleMapLink,
                    TotalFloors = building.TotalFloors,
                    ParkingDetails = building.ParkingDetails,
                    Status = building.Status,
                    Description = building.Description,
                    ImageUrl = building.ImageUrl,
                    CreatedAt = building.CreatedAt,
                    UpdatedAt = building.UpdatedAt
                };
            }
        }

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
            EntityData = entityData
        };
    }
}
