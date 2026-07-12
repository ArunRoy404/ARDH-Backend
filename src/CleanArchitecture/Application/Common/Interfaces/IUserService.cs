using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IUserService
{
    Task<List<UserViewModel>> Get(CancellationToken cancellationToken);
    Task<UserViewModel> GetById(Guid id, CancellationToken cancellationToken);
    Task Create(UserCreateRequest request, CancellationToken cancellationToken);
    Task Update(UserUpdateRequest request, CancellationToken cancellationToken);
    Task Delete(Guid userId, CancellationToken cancellationToken);
}
