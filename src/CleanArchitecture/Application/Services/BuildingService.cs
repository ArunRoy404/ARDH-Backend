using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Building;

namespace CleanArchitecture.Application.Services;

public class BuildingService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityService activityService, INotificationService notificationService) : IBuildingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<BuildingViewModel>> GetPaginated(int page, int pageSize, string? search, BuildingStatus? status, CancellationToken cancellationToken)
    {
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => !status.HasValue || x.Status == status.Value);
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var query = buildings.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                (x.BuildingName != null && x.BuildingName.ToLower().Contains(cleanSearch)) ||
                (x.City != null && x.City.ToLower().Contains(cleanSearch)) ||
                (x.State != null && x.State.ToLower().Contains(cleanSearch))
            );
        }

        var totalCount = query.Count();
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new BuildingViewModel
            {
                Id = x.Id,
                BuildingName = x.BuildingName,
                Address = x.Address,
                City = x.City,
                State = x.State,
                Country = x.Country,
                GoogleMapLink = x.GoogleMapLink,
                TotalFloors = x.TotalFloors,
                ParkingDetails = x.ParkingDetails,
                Status = x.Status,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
                UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
            })
            .ToList();

        return new PaginatedList<BuildingViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<BuildingViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");

        var createdByUser = building.CreatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == building.CreatedBy.Value) : null;
        var updatedByUser = building.UpdatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == building.UpdatedBy.Value) : null;

        return new BuildingViewModel
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
            UpdatedAt = building.UpdatedAt,
            CreatedBy = createdByUser?.Name,
            UpdatedBy = updatedByUser?.Name,
        };
    }

    public async Task Create(BuildingCreateRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = request.BuildingName?.Trim().ToLowerInvariant() ?? string.Empty;
        var isNameExist = await _unitOfWork.BuildingRepository.AnyIncludingDeletedAsync(x => x.BuildingName.Trim().ToLower() == normalizedName);
        if (isNameExist)
        {
            throw BuildingException.BadRequestException($"Building with name '{request.BuildingName}' already exists.");
        }

        var building = new Building
        {
            Id = Guid.NewGuid(),
            BuildingName = request.BuildingName,
            Address = request.Address ?? string.Empty,
            City = request.City,
            State = request.State,
            Country = request.Country,
            GoogleMapLink = request.GoogleMapLink ?? string.Empty,
            TotalFloors = request.TotalFloors ?? 0,
            ParkingDetails = request.ParkingDetails ?? string.Empty,
            Status = request.Status ?? BuildingStatus.active,
            Description = request.Description ?? string.Empty,
            ImageUrl = request.ImageUrl ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.BuildingRepository.AddAsync(building), cancellationToken);

        await _activityService.CreateActivity("Create", "Building", building.Id, building.Id, $"Building '{building.BuildingName}' was created.", cancellationToken);
        await _notificationService.CreateNotificationInternal("properties", "Building Created", $"Building '{building.BuildingName}' was created.", cancellationToken);
    }

    public async Task Update(BuildingUpdateRequest request, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");

        if (!string.Equals(building.BuildingName, request.BuildingName, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedName = request.BuildingName?.Trim().ToLowerInvariant() ?? string.Empty;
            var isNameExist = await _unitOfWork.BuildingRepository.AnyIncludingDeletedAsync(x => x.BuildingName.Trim().ToLower() == normalizedName && x.Id != request.Id);
            if (isNameExist)
            {
                throw BuildingException.BadRequestException($"Building with name '{request.BuildingName}' already exists.");
            }
        }

        building.BuildingName = request.BuildingName;
        building.City = request.City;
        building.State = request.State;
        building.Country = request.Country;

        if (request.Address != null) building.Address = request.Address;
        if (request.GoogleMapLink != null) building.GoogleMapLink = request.GoogleMapLink;
        if (request.TotalFloors.HasValue) building.TotalFloors = request.TotalFloors.Value;
        if (request.ParkingDetails != null) building.ParkingDetails = request.ParkingDetails;
        if (request.Status.HasValue) building.Status = request.Status.Value;
        if (request.Description != null) building.Description = request.Description;
        if (request.ImageUrl != null) building.ImageUrl = request.ImageUrl;

        building.UpdatedAt = DateTime.UtcNow;
        building.UpdatedBy = _currentUser.GetCurrentUserId();
 
        _unitOfWork.BuildingRepository.Update(building);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Update", "Building", building.Id, building.Id, $"Building '{building.BuildingName}' details were updated.", cancellationToken);
        await _notificationService.CreateNotificationInternal("properties", "Building Updated", $"Building '{building.BuildingName}' details were updated.", cancellationToken);
    }

    public async Task<BuildingStatsViewModel> GetStats(Guid buildingId, CancellationToken cancellationToken)
    {
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == buildingId);
        if (!buildingExists)
        {
            throw BuildingException.NotFoundException("The specified building does not exist.");
        }

        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.BuildingId == buildingId);
        var maintenanceRequests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.BuildingId == buildingId);

        var totalApartments = apartments.Count;
        var totalOccupied = apartments.Count(x => x.CurrentTenantId != null && x.CurrentTenantId != Guid.Empty);
        var totalVacant = totalApartments - totalOccupied;
        var totalOpenMaintenance = maintenanceRequests.Count(x => x.Status == MaintenanceStatus.Open);

        return new BuildingStatsViewModel
        {
            TotalApartments = totalApartments,
            TotalOccupied = totalOccupied,
            TotalVacant = totalVacant,
            TotalOpenMaintenance = totalOpenMaintenance
        };
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");
 
        building.IsDeleted = true;
        building.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.BuildingRepository.Update(building);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Building",
            EntityId = building.Id,
            EntityTitle = $"{building.BuildingName}-{building.City}-{building.State}-{building.CreatedAt}",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Delete", "Building", building.Id, building.Id, $"Building '{building.BuildingName}' was deleted.", cancellationToken);
        await _notificationService.CreateNotificationInternal("properties", "Building Deleted", $"Building '{building.BuildingName}' was deleted.", cancellationToken);
    }
}
