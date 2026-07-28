using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Activity;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IActivityService
{
    Task<PaginatedList<ActivityViewModel>> GetPaginated(
        int page,
        int pageSize,
        Guid? buildingId,
        CancellationToken cancellationToken);

    Task CreateActivity(
        string actionType,
        string entityType,
        Guid entityId,
        Guid? buildingId,
        string description,
        CancellationToken cancellationToken);
}
