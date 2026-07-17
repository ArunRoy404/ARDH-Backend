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

public class BuildingService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : IBuildingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<BuildingViewModel>> GetPaginated(int page, int pageSize, string? search, BuildingStatus? status, CancellationToken cancellationToken)
    {
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
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
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BuildingViewModel
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
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        return new PaginatedList<BuildingViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<BuildingViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");

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
            UpdatedAt = building.UpdatedAt
        };
    }

    public async Task Create(BuildingCreateRequest request, CancellationToken cancellationToken)
    {
        var isNameExist = await _unitOfWork.BuildingRepository.AnyAsync(x => x.BuildingName == request.BuildingName && x.DeletedAt == null);
        if (isNameExist)
        {
            throw BuildingException.BadRequestException($"Building with name '{request.BuildingName}' already exists.");
        }

        var building = new Building
        {
            Id = Guid.NewGuid(),
            BuildingName = request.BuildingName,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Country = request.Country,
            GoogleMapLink = request.GoogleMapLink,
            TotalFloors = request.TotalFloors,
            ParkingDetails = request.ParkingDetails,
            Status = request.Status,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.BuildingRepository.AddAsync(building), cancellationToken);
    }

    public async Task Update(BuildingUpdateRequest request, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.DeletedAt == null)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");

        if (building.BuildingName != request.BuildingName)
        {
            var isNameExist = await _unitOfWork.BuildingRepository.AnyAsync(x => x.BuildingName == request.BuildingName && x.Id != request.Id && x.DeletedAt == null);
            if (isNameExist)
            {
                throw BuildingException.BadRequestException($"Building with name '{request.BuildingName}' already exists.");
            }
        }

        building.BuildingName = request.BuildingName;
        building.Address = request.Address;
        building.City = request.City;
        building.State = request.State;
        building.Country = request.Country;
        building.GoogleMapLink = request.GoogleMapLink;
        building.TotalFloors = request.TotalFloors;
        building.ParkingDetails = request.ParkingDetails;
        building.Status = request.Status;
        building.Description = request.Description;
        building.ImageUrl = request.ImageUrl;
        building.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.BuildingRepository.Update(building);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw BuildingException.NotFoundException("The specified building does not exist.");

        building.DeletedAt = DateTime.UtcNow;
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
    }
}
