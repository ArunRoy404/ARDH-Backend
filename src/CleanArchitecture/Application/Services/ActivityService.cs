using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Activity;

namespace CleanArchitecture.Application.Services;

public class ActivityService(IUnitOfWork unitOfWork) : IActivityService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PaginatedList<ActivityViewModel>> GetPaginated(
        int page,
        int pageSize,
        Guid? buildingId,
        CancellationToken cancellationToken)
    {
        // Query database activities. Order by CreatedAt descending to get recent activities first.
        var query = await _unitOfWork.ActivityRepository.GetAllAsync();

        if (buildingId.HasValue && buildingId.Value != Guid.Empty)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value).ToList();
        }

        var totalCount = query.Count;

        var items = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ActivityViewModel
            {
                Id = x.Id,
                ActionType = x.ActionType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                BuildingId = x.BuildingId,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return new PaginatedList<ActivityViewModel>(items, totalCount, page, pageSize);
    }

    public async Task CreateActivity(
        string actionType,
        string entityType,
        Guid entityId,
        Guid? buildingId,
        string description,
        CancellationToken cancellationToken)
    {
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            ActionType = actionType.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            BuildingId = buildingId,
            Description = description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ActivityRepository.AddAsync(activity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
