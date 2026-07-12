using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IUserService
{
    Task<List<UserViewModel>> Get(CancellationToken cancellationToken);
    Task<PaginatedList<UserViewModel>> GetPaginated(int page, int pageSize, string? search, UserRole? role, bool? isActive, CancellationToken cancellationToken);
    Task<UserViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(UserCreateRequest request, CancellationToken cancellationToken);
    Task Update(UserUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid userId, CancellationToken cancellationToken);
    Task ToggleStatus(Guid id, CancellationToken cancellationToken);
}
