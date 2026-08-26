using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Utilities;

namespace CleanArchitecture.Application.Services;

/// <summary>
/// A background service that runs every 24 hours to handle system reminders:
/// 1. AMC contract expiry (1 month before)
/// 2. Tenant lease expiry (1 week before)
/// 3. Pending maintenance reminders
/// 4. Recurring maintenance auto-scheduling
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

        await ProcessAmcExpiryAsync(unitOfWork, notificationService, mailService, allUsers, cancellationToken);
        await ProcessLeaseExpiryAsync(unitOfWork, notificationService, mailService, allUsers, cancellationToken);
        await ProcessPendingMaintenanceAsync(unitOfWork, mailService, allUsers, cancellationToken);
        await ProcessRecurringMaintenanceAsync(unitOfWork, notificationService, mailService, allUsers, cancellationToken);

        _logger.LogInformation("Completed reminder scan at {Time}", DateTime.UtcNow);
    }

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
            var title = $"AMC Expiring: {contract.ContractTitle}";
            var detail = $"AMC contract '{contract.ContractTitle}' ({contract.AmcCode}) expires on {contract.EndDate:yyyy-MM-dd}.";

            // 1. Create In-App Notification (only if it doesn't already exist for this contract/title)
            var exists = await unitOfWork.NotificationRepository.AnyAsync(x => x.Type == "operations" && x.Title == title);
            if (!exists)
            {
                await notificationService.CreateNotificationInternal("operations", title, detail, cancellationToken);
            }

            // 2. Send Emails to opted-in users who have access to 'operations'
            var targetUsers = GetTargetUsers(allUsers, "operations");
            foreach (var user in targetUsers)
            {
                var wasSentToday = await CheckIfEmailSentToday(unitOfWork, "AmcExpiry", contract.Id, user.Id);
                if (!wasSentToday)
                {
                    var subject = $"Action Required: {title}";
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
            var title = $"Lease Expiring: {tenant.FullName}";
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
                    var subject = $"Action Required: {title}";
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

    private async Task ProcessPendingMaintenanceAsync(
        IUnitOfWork unitOfWork,
        IMailService mailService,
        List<User> allUsers,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await unitOfWork.MaintenanceRequestRepository.GetAllAsync(
            x => x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress);

        var setting = await unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);

        // Group by user to send a single daily digest instead of multiple emails
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

            if (requestsToSend.Any())
            {
                var subject = $"Pending Maintenance Digest ({requestsToSend.Count} tasks)";
                
                var listHtml = string.Join("", requestsToSend.Select(r => 
                    $"<li><strong>{r.Title}</strong> (Status: {r.Status}) - Priority: {r.Priority}</li>"));

                var bodyContent = $@"
                    <h2 style=""margin:0 0 8px;color:#111827;font-size:20px;"">Daily Pending Maintenance Digest</h2>
                    <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Hello {user.Name}, here is the list of currently pending maintenance requests that require attention:</p>
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
    }

    private async Task ProcessRecurringMaintenanceAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IMailService mailService,
        List<User> allUsers,
        CancellationToken cancellationToken)
    {
        // Find all completed recurring requests
        var recurringRequests = await unitOfWork.MaintenanceRequestRepository.GetAllAsync(
            x => x.Status == MaintenanceStatus.Complete && x.RecurrenceFrequency != null);

        var setting = await unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        var targetUsers = GetTargetUsers(allUsers, "operations");

        foreach (var request in recurringRequests)
        {
            var nextDate = GetNextMaintenanceDate(request.StartDate, request.RecurrenceFrequency, request.LastCompletedDate);
            
            // If the time has arrived to create a new task
            if (nextDate.HasValue && nextDate.Value.Date <= DateTime.UtcNow.Date)
            {
                // To prevent creating multiple duplicates, check if an open/in progress request
                // with the exact same title/building/equipment already exists.
                // We use title matching as a simple heuristic for recurring tasks.
                var title = request.Title.Trim();
                var duplicateExists = await unitOfWork.MaintenanceRequestRepository.AnyAsync(
                    x => x.Title == title && 
                         x.BuildingId == request.BuildingId && 
                         x.EquipmentId == request.EquipmentId &&
                         (x.Status == MaintenanceStatus.Open || x.Status == MaintenanceStatus.InProgress));

                if (!duplicateExists)
                {
                    // Create new row
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
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    var notifTitle = $"Recurring Maintenance: {newRequest.Title}";
                    var detail = $"A new maintenance task was auto-generated for '{newRequest.Title}' scheduled on {nextDate.Value:yyyy-MM-dd}.";
                    await notificationService.CreateNotificationInternal("operations", notifTitle, detail, cancellationToken);

                    foreach (var user in targetUsers)
                    {
                        var subject = $"New Auto-Generated Task: {newRequest.Title}";
                        var bodyContent = $@"
                            <h2 style=""margin:0 0 8px;color:#111827;font-size:20px;"">Recurring Maintenance Task Created</h2>
                            <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Hello {user.Name},</p>
                            <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">{detail}</p>
                            <p style=""margin:0 0 20px;color:#4b5563;font-size:14px;line-height:1.6;"">Please assign a vendor or update the status in the dashboard.</p>";
                        var htmlMessage = EmailTemplateBuilder.Build(setting?.Icon, setting?.CompanyName, bodyContent);

                        await TrySendEmailAndLogAsync(unitOfWork, mailService, user.Email, subject, htmlMessage, "RecurringMaintenance", newRequest.Id, user.Id);
                    }
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

    private static DateTime? GetNextMaintenanceDate(DateTime? startDate, MaintenanceRecurrenceFrequency? frequency, DateTime? lastCompletedDate = null)
    {
        if (!startDate.HasValue || !frequency.HasValue) return null;

        var days = frequency.Value switch
        {
            MaintenanceRecurrenceFrequency.Daily => 1,
            MaintenanceRecurrenceFrequency.Weekly => 7,
            MaintenanceRecurrenceFrequency.BiWeekly => 14,
            MaintenanceRecurrenceFrequency.Monthly => 30,
            MaintenanceRecurrenceFrequency.BiMonthly => 60,
            MaintenanceRecurrenceFrequency.Quarterly => 90,
            MaintenanceRecurrenceFrequency.HalfYearly => 182,
            MaintenanceRecurrenceFrequency.Yearly => 365,
            MaintenanceRecurrenceFrequency.BiYearly => 730,
            _ => 0
        };

        var baseDate = lastCompletedDate ?? startDate.Value;
        return baseDate.AddDays(days);
    }
}
