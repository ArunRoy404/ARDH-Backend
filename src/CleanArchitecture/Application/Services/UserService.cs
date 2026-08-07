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

public class UserService(IUnitOfWork unitOfWork, ICurrentUser currentUser, INotificationService notificationService) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<List<UserViewModel>> Get(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

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
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
        }).ToList();
    }

    public async Task<PaginatedList<UserViewModel>> GetPaginated(int page, int pageSize, string? search, UserRole? role, bool? isActive, CancellationToken cancellationToken)
    {
        var allUsers = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = allUsers.ToDictionary(u => u.Id, u => u.Name);
        var query = allUsers.AsQueryable();

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
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new UserViewModel
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
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
                UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
            })
            .ToList();

        return new PaginatedList<UserViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<UserViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        var createdByUser = user.CreatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == user.CreatedBy.Value) : null;
        var updatedByUser = user.UpdatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == user.UpdatedBy.Value) : null;

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
            UpdatedAt = user.UpdatedAt,
            CreatedBy = createdByUser?.Name,
            UpdatedBy = updatedByUser?.Name,
        };
    }

    private static readonly Dictionary<UserRole, UserPermission[]> DefaultRolePermissions = new()
    {
        [UserRole.admin] = Enum.GetValues<UserPermission>(),
        // Viewer is read-only: gets all module permissions for viewing but never admin.
        [UserRole.viewer] = [UserPermission.dashboard, UserPermission.properties, UserPermission.finance, UserPermission.operations],
        [UserRole.property_manager] = [UserPermission.operations],
        [UserRole.accountant] = [UserPermission.finance],
    };

    private static string ResolvePermissions(UserRole role, string? requestedPermissions)
    {
        var requested = (requestedPermissions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => Enum.TryParse<UserPermission>(p, true, out _))
            .Select(p => Enum.Parse<UserPermission>(p, true));

        var permissions = new HashSet<UserPermission>(
            DefaultRolePermissions.TryGetValue(role, out var defaults) ? defaults : []);
        permissions.UnionWith(requested);

        return string.Join(",", Enum.GetValues<UserPermission>().Where(permissions.Contains));
    }

    public async Task Create(UserCreateRequest request, CancellationToken cancellationToken)
    {
        var isEmailExist = await _unitOfWork.UserRepository.AnyIncludingDeletedAsync(x => x.Email == request.Email);
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
            Permissions = ResolvePermissions(request.Role, request.Permissions),
            AvatarUrl = request.AvatarUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.UserRepository.AddAsync(user), cancellationToken);

        await _notificationService.CreateNotificationInternal("admin", "New User Created", $"User '{user.Name}' ({user.Role}) was created.", cancellationToken);
    }

    public async Task Update(UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        // Check if email is updated to an existing one
        if (user.Email != request.Email)
        {
            var isEmailExist = await _unitOfWork.UserRepository.AnyIncludingDeletedAsync(x => x.Email == request.Email && x.Id != request.Id);
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
        user.Permissions = ResolvePermissions(request.Role, request.Permissions);
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("admin", "User Updated", $"User '{user.Name}' details were updated.", cancellationToken);
    }

    public async Task Delete(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        user.IsActive = false;
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.UserRepository.Update(user);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "User",
            EntityId = user.Id,
            EntityTitle = $"{user.Name}-{user.Email}-{user.Phone}-{user.CreatedAt}",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("admin", "User Deleted", $"User '{user.Name}' was deleted.", cancellationToken);
    }

    public async Task ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw UserException.BadRequestException("The specified user does not exist.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var statusText = user.IsActive ? "activated" : "deactivated";
        await _notificationService.CreateNotificationInternal("admin", "User Status Changed", $"User '{user.Name}' was {statusText}.", cancellationToken);
    }
}
