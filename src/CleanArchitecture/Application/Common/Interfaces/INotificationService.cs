using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Notification;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface INotificationService
{
    Task<PaginatedList<NotificationViewModel>> GetNotifications(
        int page,
        int pageSize,
        string? type,
        bool? isRead,
        CancellationToken cancellationToken);

    Task<NotificationCountViewModel> GetCount(CancellationToken cancellationToken);

    Task MarkAsRead(Guid id, CancellationToken cancellationToken);

    Task MarkAllAsRead(CancellationToken cancellationToken);

    Task CreateNotification(NotificationCreateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    Task ClearAll(CancellationToken cancellationToken);

    Task CreateNotificationInternal(string type, string title, string detail, CancellationToken cancellationToken);
}
