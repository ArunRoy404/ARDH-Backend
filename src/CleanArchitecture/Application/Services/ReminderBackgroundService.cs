using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Services;

/// <summary>
/// A background service that runs every 24 hours to handle system reminders:
/// 1. Recurring maintenance rollover - auto-creates the next cycle once a completed recurring
///    request's interval has elapsed (bounded: each completed request is only ever evaluated
///    until its successor exists, via NextCycleGenerated).
/// 2. Maintenance due transition - flips an Open recurring request to Pending once its StartDate
///    arrives (whether just auto-created above, or a manually created recurring request coming
///    due for the first time), firing a one-time in-app notification.
/// 3. Pending maintenance digest - one daily email per user listing everything still Pending,
///    repeating until each item is moved off Pending.
/// 4. AMC contract expiry (1 month before).
/// 5. Tenant lease expiry (1 week before).
/// All email sends are gated by User.ReceiveEmailNotifications and the same module-permission
/// matching used for in-app notifications (see HasPermissionForType).
/// </summary>
public class ReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger = logger;

    // Run every 24 hours
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started. Interval: {Interval}", _checkInterval);

        // Optional: wait a few seconds before the first run so the app can start up fully
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing background reminders.");
            }

            // Wait until next cycle
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reminder scan at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

        var allUsers = await unitOfWork.UserRepository.GetAllAsync(x => x.IsActive);

        // Order matters: a freshly auto-created next-cycle row's StartDate is already <= today by
        // construction, so running the due-transition step right after lets it flip Open -> Pending
        // in the SAME tick instead of waiting a full extra day; the Pending digest then picks it up
        // immediately too.
        await ProcessRecurringMaintenanceRolloverAsync(unitOfWork, cancellationToken);
        await ProcessMaintenanceDueTransitionAsync(unitOfWork, notificationService, cancellationToken);
        await ProcessPendingMaintenanceAsync(unitOfWork, mailService, allUsers, cancellationToken);
        await ProcessAmcExpiryAsync(unitOfWork, notificationService, mailService, allUsers, cancellationToken);
        await ProcessLeaseExpiryAsync(unitOfWork, notificationService, mailService, allUsers, cancellationToken);

        _logger.LogInformation("Completed reminder scan at {Time}", DateTime.UtcNow);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recurring maintenance: auto-create the next cycle once a completed recurring request's
    // interval has elapsed.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ProcessRecurringMaintenanceRolloverAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        // Bounded by NextCycleGenerated: once a completed row's successor exists (or is created
        // below), it's excluded from every future scan forever - instead of re-scanning the whole
        // historical chain of completed cycles every day.
        var recurringRequests = await unitOfWork.MaintenanceRequestRepository.GetAllAsync(
            x => x.Status == MaintenanceStatus.Complete && x.RecurrenceFrequency != null && !x.NextCycleGenerated);

        foreach (var request in recurringRequests)
        {
            var nextDate = MaintenanceRecurrenceHelper.GetNextOccurrence(
                request.LastCompletedDate ?? request.StartDate, request.RecurrenceFrequency);

            if (!nextDate.HasValue || nextDate.Value.Date > DateTime.UtcNow.Date)
            {
                continue;
            }

            var title = request.Title.Trim();
            var duplicateExists = await unitOfWork.MaintenanceRequestRepository.AnyAsync(
                x => x.Title == title &&
                     x.BuildingId == request.BuildingId &&
                     x.EquipmentId == request.EquipmentId &&
                     (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress || x.Status == MaintenanceStatus.Pending));

            if (!duplicateExists)
            {
                var newRequest = new MaintenanceRequest
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Description = request.Description,
                    Category = request.Category,
                    Priority = request.Priority,
                    Status = MaintenanceStatus.Open,
                    VendorId = request.VendorId,
                    EquipmentId = request.EquipmentId,
                    BuildingId = request.BuildingId,
                    ApartmentId = request.ApartmentId,
                    EstimatedCost = request.EstimatedCost,
                    AnnualCost = request.AnnualCost,
                    ScheduledDate = nextDate,
                    StartDate = nextDate,
                    RecurrenceFrequency = request.RecurrenceFrequency,
                    Notes = "Auto-generated from recurring maintenance schedule.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty, // System generated
                };

                await unitOfWork.MaintenanceRequestRepository.AddAsync(newRequest);
            }

            // Mark the completed row handled either way - if an active descendant already exists
            // (e.g. a prior run created it but crashed before this flag was saved), this
            // predecessor's job is done and it must never be re-evaluated again.
            request.NextCycleGenerated = true;
            unitOfWork.MaintenanceRequestRepository.Update(request);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Flip Open recurring requests to Pending once their StartDate arrives - covers both rows
    // just auto-created above and any manually created recurring request reaching its first due
    // date. Fires a one-time in-app notification for the transition (naturally idempotent: once
    // flipped, the row is no longer Open and won't be picked up again). The daily email reminder
    // is handled by ProcessPendingMaintenanceAsync below.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ProcessMaintenanceDueTransitionAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var dueRequests = await unitOfWork.MaintenanceRequestRepository.GetAllAsync(
            x => x.Status == MaintenanceStatus.Open &&
                 x.RecurrenceFrequency != null &&
                 x.StartDate.HasValue &&
                 x.StartDate.Value.Date <= today);

        foreach (var request in dueRequests)
        {
            request.Status = MaintenanceStatus.Pending;
            request.UpdatedAt = DateTime.UtcNow;

            if (request.EquipmentId.HasValue)
            {
                var equipment = await unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value);
                if (equipment != null)
                {
                    equipment.Status = "UnderMaintenance";
                    equipment.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.EquipmentRepository.Update(equipment);
                }
            }

            unitOfWork.MaintenanceRequestRepository.Update(request);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.CreateNotificationInternal(
                "operations",
                $"Maintenance Due: {request.Title}",
                $"'{request.Title}' has come due and is now pending action.",
                cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Daily digest email for everything currently Pending - one email per user per day listing
    // every due item they haven't already been emailed about today, not one email per request.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ProcessPendingMaintenanceAsync(
        IUnitOfWork unitOfWork,
        IMailService mailService,
        List<User> allUsers,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await unitOfWork.MaintenanceRequestRepository.GetAllAsync(
            x => x.Status == MaintenanceStatus.Pending);

        if (pendingRequests.Count == 0)
        {
            return;
        }

        var setting = await unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        var targetUsers = GetTargetUsers(allUsers, "operations");

        foreach (var user in targetUsers)
        {
            var requestsToSend = new List<MaintenanceRequest>();

            foreach (var request in pendingRequests)
            {
                var wasSentToday = await CheckIfEmailSentToday(unitOfWork, "PendingMaintenance", request.Id, user.Id);
                if (!wasSentToday)
                {
                    requestsToSend.Add(request);
                }
            }

            if (requestsToSend.Count == 0)
            {
                continue;
            }

            var subject = $"Pending Maintenance Digest ({requestsToSend.Count} task{(requestsToSend.Count == 1 ? "" : "s")})";

            var listHtml = string.Join("", requestsToSend.Select(r =>
                $"<li><strong>{r.Title}</strong> (Priority: {r.Priority})</li>"));

            var bodyContent = $@"
                <h2 style=""margin:0 0 8px;color:#111827;font-size:20px;"">Daily Pending Maintenance Digest</h2>
                <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Hello {user.Name}, the following maintenance requests are due and awaiting action:</p>
                <ul>{listHtml}</ul>";
            var htmlMessage = EmailTemplateBuilder.Build(setting?.Icon, setting?.CompanyName, bodyContent);

            try
            {
                await mailService.SendEmailAsync(user.Email, subject, htmlMessage);

                // Log all of them so they aren't sent again today
                foreach (var request in requestsToSend)
                {
                    await LogEmailSentAsync(unitOfWork, "PendingMaintenance", request.Id, user.Id);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send pending maintenance digest to {email}", user.Email);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AMC contract expiry (1 month before). The notification title embeds the contract's AmcCode
    // (unique) and current EndDate, so a renewal (new EndDate) produces a different title and
    // fires again - unlike a plain title match, which would suppress the alert forever after the
    // first fire even across renewals.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ProcessAmcExpiryAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IMailService mailService,
        List<User> allUsers,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(30); // 1 month before

        var contracts = await unitOfWork.AmcContractRepository.GetAllAsync(
            x => x.Status != AmcStatus.Cancelled &&
                 x.Status != AmcStatus.Expired &&
                 x.EndDate > now &&
                 x.EndDate <= threshold);

        var setting = await unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);

        foreach (var contract in contracts)
        {
            var title = $"AMC Expiring: {contract.ContractTitle} ({contract.AmcCode}) - due {contract.EndDate:yyyy-MM-dd}";
            var detail = $"AMC contract '{contract.ContractTitle}' ({contract.AmcCode}) expires on {contract.EndDate:yyyy-MM-dd}.";

            // 1. Create In-App Notification (only if it doesn't already exist for this exact cycle)
            var exists = await unitOfWork.NotificationRepository.AnyAsync(x => x.Type == "operations" && x.Title == title);
            if (!exists)
            {
                await notificationService.CreateNotificationInternal("operations", title, detail, cancellationToken);
            }

            // 2. Send emails to opted-in users who have access to 'operations'
            var targetUsers = GetTargetUsers(allUsers, "operations");
            foreach (var user in targetUsers)
            {
                var wasSentToday = await CheckIfEmailSentToday(unitOfWork, "AmcExpiry", contract.Id, user.Id);
                if (!wasSentToday)
                {
                    var subject = $"Action Required: AMC Expiring: {contract.ContractTitle}";
                    var bodyContent = $@"
                        <h2 style=""margin:0 0 8px;color:#111827;font-size:20px;"">AMC Contract Expiry Notice</h2>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Hello {user.Name},</p>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">{detail}</p>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Please take necessary actions to renew or close the contract.</p>";
                    var htmlMessage = EmailTemplateBuilder.Build(setting?.Icon, setting?.CompanyName, bodyContent);

                    await TrySendEmailAndLogAsync(unitOfWork, mailService, user.Email, subject, htmlMessage, "AmcExpiry", contract.Id, user.Id);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tenant lease expiry (1 week before). Same renewal-safe title fix as AMC above (embeds the
    // flat number and current LeaseEndDate).
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ProcessLeaseExpiryAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IMailService mailService,
        List<User> allUsers,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var thresholdDate = today.AddDays(7); // 1 week before

        var activeTenants = await unitOfWork.TenantRepository.GetAllAsync(
            x => x.Status == TenantStatus.Active &&
                 x.LeaseEndDate.HasValue &&
                 x.LeaseEndDate.Value.Date >= today &&
                 x.LeaseEndDate.Value.Date <= thresholdDate);

        var setting = await unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);

        foreach (var tenant in activeTenants)
        {
            var leaseEndDateStr = tenant.LeaseEndDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            var apartment = tenant.ApartmentId != Guid.Empty
                ? await unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == tenant.ApartmentId)
                : null;
            var flatLabel = apartment?.FlatNumber ?? "Unknown Flat";
            var title = $"Lease Expiring: {tenant.FullName} - Flat {flatLabel} - due {leaseEndDateStr}";
            var detail = $"Lease for {tenant.FullName} is expiring on {leaseEndDateStr}. Flat: {flatLabel}.";

            var exists = await unitOfWork.NotificationRepository.AnyAsync(x => x.Type == "properties" && x.Title == title);
            if (!exists)
            {
                await notificationService.CreateNotificationInternal("properties", title, detail, cancellationToken);
            }

            var targetUsers = GetTargetUsers(allUsers, "properties");
            foreach (var user in targetUsers)
            {
                var wasSentToday = await CheckIfEmailSentToday(unitOfWork, "LeaseExpiry", tenant.Id, user.Id);
                if (!wasSentToday)
                {
                    var subject = $"Action Required: Lease Expiring: {tenant.FullName}";
                    var bodyContent = $@"
                        <h2 style=""margin:0 0 8px;color:#111827;font-size:20px;"">Tenant Lease Expiry Notice</h2>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Hello {user.Name},</p>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">{detail}</p>
                        <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Please contact the tenant to process renewal or move-out.</p>";
                    var htmlMessage = EmailTemplateBuilder.Build(setting?.Icon, setting?.CompanyName, bodyContent);

                    await TrySendEmailAndLogAsync(unitOfWork, mailService, user.Email, subject, htmlMessage, "LeaseExpiry", tenant.Id, user.Id);
                }
            }
        }
    }

    private static List<User> GetTargetUsers(List<User> users, string requiredPermissionType)
    {
        return users
            .Where(u => u.ReceiveEmailNotifications && HasPermissionForType(u, requiredPermissionType))
            .ToList();
    }

    private static bool HasPermissionForType(User user, string type)
    {
        var isAdminRole = user.Role == UserRole.admin;
        var permissionsList = (user.Permissions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        var hasAdminPermission = isAdminRole || permissionsList.Contains("admin");
        if (hasAdminPermission) return true;

        return type.ToLowerInvariant() switch
        {
            "operations" => permissionsList.Contains("vendors") || permissionsList.Contains("equipment") ||
                             permissionsList.Contains("amc_contracts") || permissionsList.Contains("maintenance"),
            "finance" => permissionsList.Contains("income") || permissionsList.Contains("reports") ||
                         permissionsList.Contains("expenses"),
            "properties" => permissionsList.Contains("buildings") || permissionsList.Contains("owners") ||
                             permissionsList.Contains("apartments") || permissionsList.Contains("tenants"),
            "admin" => false,
            _ => permissionsList.Contains("dashboard")
        };
    }

    private static async Task<bool> CheckIfEmailSentToday(IUnitOfWork unitOfWork, string reminderType, Guid entityId, Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        return await unitOfWork.EmailReminderLogRepository.AnyAsync(
            x => x.ReminderType == reminderType &&
                 x.EntityId == entityId &&
                 x.UserId == userId &&
                 x.SentAt >= today);
    }

    private static async Task LogEmailSentAsync(IUnitOfWork unitOfWork, string reminderType, Guid entityId, Guid userId)
    {
        var log = new EmailReminderLog
        {
            Id = Guid.NewGuid(),
            ReminderType = reminderType,
            EntityId = entityId,
            UserId = userId,
            SentAt = DateTime.UtcNow
        };
        await unitOfWork.EmailReminderLogRepository.AddAsync(log);
    }

    private async Task TrySendEmailAndLogAsync(
        IUnitOfWork unitOfWork,
        IMailService mailService,
        string email,
        string subject,
        string htmlMessage,
        string reminderType,
        Guid entityId,
        Guid userId)
    {
        try
        {
            await mailService.SendEmailAsync(email, subject, htmlMessage);
            await LogEmailSentAsync(unitOfWork, reminderType, entityId, userId);
            await unitOfWork.SaveChangesAsync(default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reminder email of type {Type} to {Email}", reminderType, email);
        }
    }
}
