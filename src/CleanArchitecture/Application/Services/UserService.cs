using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Services;

public class UserService(IUnitOfWork unitOfWork) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<List<UserViewModel>> Get(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync(x => x.DeletedAt == null);

        return users.Select(x => new UserViewModel
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            Address = x.Address,
            Role = x.Role,
            AvatarUrl = x.AvatarUrl,
            IsActive = x.IsActive,
            Permissions = x.Permissions,
            LastLoginAt = x.LastLoginAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();
    }

    public async Task<PaginatedList<UserViewModel>> GetPaginated(int page, int pageSize, string? search, UserRole? role, bool? isActive, CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync(x => x.DeletedAt == null);
        var query = users.AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(x => x.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(cleanSearch)) ||
                (x.Email != null && x.Email.ToLower().Contains(cleanSearch)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(cleanSearch))
            );
        }

        var totalCount = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address ?? string.Empty,
                Role = x.Role,
                AvatarUrl = x.AvatarUrl ?? string.Empty,
                IsActive = x.IsActive,
                Permissions = x.Permissions ?? string.Empty,
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        return new PaginatedList<UserViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<UserViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        return new UserViewModel
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

    public async Task Create(UserCreateRequest request, CancellationToken cancellationToken)
    {
        var isEmailExist = await _unitOfWork.UserRepository.AnyAsync(x => x.Email == request.Email);
        if (isEmailExist)
        {
            throw UserException.UserAlreadyExistsException(request.Email);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = request.Password.Hash(),
            Address = request.Address,
            Role = request.Role,
            Permissions = request.Permissions,
            AvatarUrl = request.AvatarUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.UserRepository.AddAsync(user), cancellationToken);
    }

    public async Task Update(UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.DeletedAt == null)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        // Check if email is updated to an existing one
        if (user.Email != request.Email)
        {
            var isEmailExist = await _unitOfWork.UserRepository.AnyAsync(x => x.Email == request.Email && x.Id != request.Id);
            if (isEmailExist)
            {
                throw UserException.UserAlreadyExistsException(request.Email);
            }
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Address = request.Address;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.Permissions = request.Permissions;
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        // Soft delete the user
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
