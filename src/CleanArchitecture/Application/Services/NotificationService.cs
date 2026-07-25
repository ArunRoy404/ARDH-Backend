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
using CleanArchitecture.Shared.Models.Notification;

namespace CleanArchitecture.Application.Services;

public class NotificationService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<NotificationViewModel>> GetNotifications(
        int page,
        int pageSize,
        string? type,
        bool? isRead,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Dynamically scan for expiring leases to keep notifications synchronized
        await ScanAndGenerateLeaseExpiryNotifications(cancellationToken);

        var currentUserId = _currentUser.GetCurrentUserId();

        var recipients = await _unitOfWork.NotificationRecipientRepository.GetAllAsync(x => x.UserId == currentUserId);
        var notifications = await _unitOfWork.NotificationRepository.GetAllAsync();

        var query = recipients
            .Join(notifications,
                  r => r.NotificationId,
                  n => n.Id,
                  (r, n) => new { Recipient = r, Notification = n })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            var cleanType = type.Trim().ToLowerInvariant();
            query = query.Where(x => x.Notification.Type.ToLower() == cleanType);
        }

        if (isRead.HasValue)
        {
            query = query.Where(x => x.Recipient.IsRead == isRead.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(x => x.Notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationViewModel
            {
                Id = x.Notification.Id,
                Type = x.Notification.Type,
                Title = x.Notification.Title,
                Detail = x.Notification.Detail,
                CreatedAt = x.Notification.CreatedAt,
                IsRead = x.Recipient.IsRead,
                ReadAt = x.Recipient.ReadAt
            })
            .ToList();

        return new PaginatedList<NotificationViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<NotificationCountViewModel> GetCount(CancellationToken cancellationToken)
    {
        // Dynamically scan for expiring leases to keep notifications synchronized
        await ScanAndGenerateLeaseExpiryNotifications(cancellationToken);

        var currentUserId = _currentUser.GetCurrentUserId();

        var recipients = await _unitOfWork.NotificationRecipientRepository.GetAllAsync(x => x.UserId == currentUserId);

        var totalCount = recipients.Count;
        var unreadCount = recipients.Count(x => !x.IsRead);

        return new NotificationCountViewModel
        {
            TotalCount = totalCount,
            UnreadCount = unreadCount
        };
    }

    public async Task MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.GetCurrentUserId();

        var recipient = await _unitOfWork.NotificationRecipientRepository.FirstOrDefaultAsync(
            x => x.NotificationId == id && x.UserId == currentUserId)
            ?? throw NotFoundException($"Notification assignment with ID '{id}' was not found for the current user.");

        recipient.IsRead = true;
        recipient.ReadAt = DateTime.UtcNow;

        _unitOfWork.NotificationRecipientRepository.Update(recipient);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsRead(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.GetCurrentUserId();

        var unreadRecipients = await _unitOfWork.NotificationRecipientRepository.GetAllAsync(
            x => x.UserId == currentUserId && !x.IsRead);

        if (unreadRecipients.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var recipient in unreadRecipients)
            {
                recipient.IsRead = true;
                recipient.ReadAt = now;
            }

            _unitOfWork.NotificationRecipientRepository.UpdateRange(unreadRecipients);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CreateNotification(NotificationCreateRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        
        await CreateNotificationInternal(request.Type, request.Title, request.Detail, cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.GetCurrentUserId();

        var recipient = await _unitOfWork.NotificationRecipientRepository.FirstOrDefaultAsync(
            x => x.NotificationId == id && x.UserId == currentUserId)
            ?? throw NotFoundException($"Notification assignment with ID '{id}' was not found for the current user.");

        _unitOfWork.NotificationRecipientRepository.Delete(recipient);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAll(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.GetCurrentUserId();

        var recipients = await _unitOfWork.NotificationRecipientRepository.GetAllAsync(x => x.UserId == currentUserId);

        if (recipients.Any())
        {
            _unitOfWork.NotificationRecipientRepository.DeleteRange(recipients);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CreateNotificationInternal(string type, string title, string detail, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Type = type.Trim(),
            Title = title.Trim(),
            Detail = detail.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.NotificationRepository.AddAsync(notification);

        // Resolve active users based on role permissions
        var activeUsers = await _unitOfWork.UserRepository.GetAllAsync(x => x.IsActive);
        var recipients = new List<NotificationRecipient>();

        foreach (var user in activeUsers)
        {
            if (HasPermissionForType(user, type))
            {
                recipients.Add(new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    UserId = user.Id,
                    IsRead = false,
                    ReadAt = null
                });
            }
        }

        if (recipients.Any())
        {
            await _unitOfWork.NotificationRecipientRepository.AddRangeAsync(recipients);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ScanAndGenerateLeaseExpiryNotifications(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var thresholdDate = today.AddDays(30);

        // Find all active tenants whose lease ends in the next 30 days
        var activeTenants = await _unitOfWork.TenantRepository.GetAllAsync(
            x => x.Status == TenantStatus.Active && 
                 x.LeaseEndDate.HasValue && 
                 x.LeaseEndDate.Value.Date >= today && 
                 x.LeaseEndDate.Value.Date <= thresholdDate);

        foreach (var tenant in activeTenants)
        {
            var leaseEndDateStr = tenant.LeaseEndDate.Value.ToString("yyyy-MM-dd");
            var title = $"Lease Expiring: {tenant.FullName}";
            var detail = $"Lease for {tenant.FullName} is expiring on {leaseEndDateStr}. Flat: {tenant.ApartmentId}.";

            // Verify if we have already generated a notification for this specific title and type
            var exists = await _unitOfWork.NotificationRepository.AnyAsync(
                x => x.Type == "properties" && x.Title == title);

            if (!exists)
            {
                // Trigger properties notification
                await CreateNotificationInternal("properties", title, detail, cancellationToken);
            }
        }
    }

    private static bool HasPermissionForType(User user, string type)
    {
        if (!user.IsActive) return false;

        var isAdminRole = user.Role == UserRole.admin;
        var isPropertyManagerRole = user.Role == UserRole.property_manager;
        var permissionsList = (user.Permissions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        var hasAdminPermission = isAdminRole || permissionsList.Contains("admin");

        if (hasAdminPermission) return true;

        return type.ToLowerInvariant() switch
        {
            "operations" => isPropertyManagerRole || permissionsList.Contains("operations") || permissionsList.Contains("operation"),
            "finance" => isPropertyManagerRole || permissionsList.Contains("finance"),
            "properties" => isPropertyManagerRole || permissionsList.Contains("properties") || permissionsList.Contains("property"),
            _ => isPropertyManagerRole || permissionsList.Contains("dashboard") // Default general / dashboard
        };
    }

    private static Exception NotFoundException(string message)
    {
        // Follow the codebase exception structure (e.g. keyNotFound or custom ApiException)
        return new KeyNotFoundException(message);
    }
}
