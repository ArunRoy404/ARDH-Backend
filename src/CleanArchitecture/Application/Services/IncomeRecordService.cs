using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Tenant;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Application.Services;

public class IncomeRecordService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    INotificationService notificationService,
    IActivityService activityService) : IIncomeRecordService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IActivityService _activityService = activityService;

    public async Task<PaginatedList<IncomeRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        Guid? tenantId,
        Guid? apartmentId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var tenantMap = tenants.ToDictionary(t => t.Id, t => t.FullName);

        var query = records.AsQueryable();

        if (incomeType.HasValue)
        {
            query = query.Where(x => x.IncomeType == incomeType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        if (apartmentId.HasValue)
        {
            query = query.Where(x => x.ApartmentId == apartmentId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Period.ToLower().Contains(searchLower) ||
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (x.TransactionReference != null && x.TransactionReference.ToLower().Contains(searchLower)) ||
                (x.BuildingId.HasValue && buildingMap.ContainsKey(x.BuildingId.Value) && buildingMap[x.BuildingId.Value].ToLower().Contains(searchLower)) ||
                (x.TenantId.HasValue && tenantMap.ContainsKey(x.TenantId.Value) && tenantMap[x.TenantId.Value].ToLower().Contains(searchLower)) ||
                (x.ApartmentId.HasValue && apartmentMap.ContainsKey(x.ApartmentId.Value) && apartmentMap[x.ApartmentId.Value].ToLower().Contains(searchLower))
            );
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new IncomeRecordViewModel
        {
            Id = x.Id,
            IncomeEntity = x.IncomeEntity,
            IncomeType = x.IncomeType,
            Amount = x.Amount,
            TenantId = x.TenantId,
            TenantName = x.TenantId.HasValue && tenantMap.TryGetValue(x.TenantId.Value, out var tName) ? tName : null,
            BuildingId = x.BuildingId,
            BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
            Period = x.Period,
            PaymentDate = x.PaymentDate,
            PaymentMethod = x.PaymentMethod,
            TransactionReference = x.TransactionReference,
            Status = x.Status,
            Notes = x.Notes,
            AttachmentUrl = x.AttachmentUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
        }).ToList();

        return new PaginatedList<IncomeRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<IncomeRecordViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value) : null;
        var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value) : null;
        var tenant = record.TenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value) : null;
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var currentTenant = apartment != null && apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;

        return new IncomeRecordViewModel
        {
            Id = record.Id,
            IncomeEntity = record.IncomeEntity,
            IncomeType = record.IncomeType,
            Amount = record.Amount,
            TenantId = record.TenantId,
            TenantName = tenant != null ? tenant.FullName : null,
            Tenant = tenant == null ? null : new TenantViewModel
            {
                Id = tenant.Id,
                BuildingId = tenant.BuildingId,
                ApartmentId = tenant.ApartmentId,
                FullName = tenant.FullName,
                Phone = tenant.Phone,
                Email = tenant.Email,
                IdType = tenant.IdType,
                IdNumber = tenant.IdNumber,
                IdProofAttachmentUrl = tenant.IdProofAttachmentUrl,
                MoveInDate = tenant.MoveInDate,
                LeaseStartDate = tenant.LeaseStartDate,
                LeaseEndDate = tenant.LeaseEndDate,
                MonthlyRent = tenant.MonthlyRent,
                SecurityDeposit = tenant.SecurityDeposit,
                EmergencyContactName = tenant.EmergencyContactName,
                EmergencyContactPhone = tenant.EmergencyContactPhone,
                Status = tenant.Status,
                Notes = tenant.Notes,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt,
                CreatedBy = tenant.CreatedBy.HasValue && userMap.TryGetValue(tenant.CreatedBy.Value, out var tcb) ? tcb : null,
                UpdatedBy = tenant.UpdatedBy.HasValue && userMap.TryGetValue(tenant.UpdatedBy.Value, out var tub) ? tub : null
            },
            BuildingId = record.BuildingId,
            BuildingName = building?.BuildingName,
            Building = building == null ? null : new BuildingViewModel
            {
                Id = building.Id,
                BuildingName = building.BuildingName,
                Address = building.Address,
                City = building.City,
                State = building.State,
                Country = building.Country,
                GoogleMapLink = building.GoogleMapLink,
                TotalFloors = building.TotalFloors,
                ParkingDetails = building.ParkingDetails,
                Status = building.Status,
                Description = building.Description,
                ImageUrl = building.ImageUrl,
                CreatedAt = building.CreatedAt,
                UpdatedAt = building.UpdatedAt,
                CreatedBy = building.CreatedBy.HasValue && userMap.TryGetValue(building.CreatedBy.Value, out var bcb) ? bcb : null,
                UpdatedBy = building.UpdatedBy.HasValue && userMap.TryGetValue(building.UpdatedBy.Value, out var bub) ? bub : null
            },
            ApartmentId = record.ApartmentId,
            FlatNumber = apartment?.FlatNumber,
            Apartment = apartment == null ? null : new ApartmentViewModel
            {
                Id = apartment.Id,
                BuildingId = apartment.BuildingId,
                OwnerId = apartment.OwnerId,
                NestawayId = apartment.NestawayId,
                FlatNumber = apartment.FlatNumber,
                Floor = apartment.Floor,
                ApartmentType = apartment.ApartmentType,
                AreaSqft = apartment.AreaSqft,
                Bedrooms = apartment.Bedrooms,
                Bathrooms = apartment.Bathrooms,
                HasBalcony = apartment.HasBalcony,
                ParkingSlot = apartment.ParkingSlot,
                ExpectedRent = apartment.ExpectedRent,
                MaintenanceCharge = apartment.MaintenanceCharge,
                WaterCharge = apartment.WaterCharge,
                CurrentTenantId = apartment.CurrentTenantId,
                CurrentTenantName = currentTenant?.FullName,
                CreatedAt = apartment.CreatedAt,
                UpdatedAt = apartment.UpdatedAt,
                CreatedBy = apartment.CreatedBy.HasValue && userMap.TryGetValue(apartment.CreatedBy.Value, out var acb) ? acb : null,
                UpdatedBy = apartment.UpdatedBy.HasValue && userMap.TryGetValue(apartment.UpdatedBy.Value, out var aub) ? aub : null
            },
            Period = record.Period,
            PaymentDate = record.PaymentDate,
            PaymentMethod = record.PaymentMethod,
            TransactionReference = record.TransactionReference,
            Status = record.Status,
            Notes = record.Notes,
            AttachmentUrl = record.AttachmentUrl,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy.HasValue && userMap.TryGetValue(record.CreatedBy.Value, out var rcb) ? rcb : null,
            UpdatedBy = record.UpdatedBy.HasValue && userMap.TryGetValue(record.UpdatedBy.Value, out var rub) ? rub : null,
        };
    }

    public async Task Create(IncomeRecordCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await _unitOfWork.TenantRepository.AnyAsync(x => x.Id == request.TenantId.Value);
            if (!tenantExists)
            {
                throw IncomeRecordException.BadRequestException("The specified tenant does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        var record = new IncomeRecord
        {
            Id = Guid.NewGuid(),
            IncomeEntity = request.IncomeEntity,
            IncomeType = request.IncomeType,
            Amount = request.Amount,
            TenantId = request.TenantId,
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            Period = request.Period.Trim(),
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = request.TransactionReference?.Trim(),
            Status = request.Status,
            Notes = request.Notes?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.IncomeRecordRepository.AddAsync(record), cancellationToken);

        if (record.Status == IncomeStatus.Paid)
        {
            var tenant = record.TenantId.HasValue
                ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value)
                : null;
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;
            
            var tenantName = tenant?.FullName ?? "Tenant";
            var flatStr = apartment != null ? $" (Flat {apartment.FlatNumber})" : "";
            
            await _activityService.CreateActivity(
                "Create",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"Payment for period '{record.Period}' is overdue. Amount: {record.Amount}.",
                cancellationToken);
        }
    }

    public async Task Update(Guid id, IncomeRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var oldStatus = record.Status;

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await _unitOfWork.TenantRepository.AnyAsync(x => x.Id == request.TenantId.Value);
            if (!tenantExists)
            {
                throw IncomeRecordException.BadRequestException("The specified tenant does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        record.IncomeEntity = request.IncomeEntity;
        record.IncomeType = request.IncomeType;
        record.Amount = request.Amount;
        record.TenantId = request.TenantId;
        record.BuildingId = request.BuildingId;
        record.ApartmentId = request.ApartmentId;
        record.Period = request.Period.Trim();
        record.PaymentDate = request.PaymentDate;
        record.PaymentMethod = request.PaymentMethod;
        record.TransactionReference = request.TransactionReference?.Trim();
        record.Status = request.Status;
        record.Notes = request.Notes?.Trim();
        record.AttachmentUrl = request.AttachmentUrl?.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.IncomeRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == IncomeStatus.Paid && oldStatus != IncomeStatus.Paid)
        {
            var tenant = record.TenantId.HasValue
                ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value)
                : null;
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;
            
            var tenantName = tenant?.FullName ?? "Tenant";
            var flatStr = apartment != null ? $" (Flat {apartment.FlatNumber})" : "";
            
            await _activityService.CreateActivity(
                "Update",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue && oldStatus != IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"Payment for period '{record.Period}' is overdue. Amount: {record.Amount}.",
                cancellationToken);
        }
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        record.IsDeleted = true;
        record.UpdatedAt = now;
        _unitOfWork.IncomeRecordRepository.Update(record);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "IncomeRecord",
            EntityId = record.Id,
            EntityTitle = $"{record.IncomeType} ({record.Amount:F2} SAR)",
            DeletedBy = userId,
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity(
            "Delete",
            "IncomeRecord",
            record.Id,
            record.BuildingId,
            $"Income record of {record.Amount:N0} deleted.",
            cancellationToken);

        await _notificationService.CreateNotificationInternal(
            "finance",
            "Income Record Deleted",
            $"Income record of {record.Amount:N0} deleted.",
            cancellationToken);
    }

    public async Task UpdateStatus(Guid id, IncomeRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();
        var oldStatus = record.Status;

        record.Status = request.Status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.IncomeRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == IncomeStatus.Paid && oldStatus != IncomeStatus.Paid)
        {
            var tenant = record.TenantId.HasValue
                ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value)
                : null;
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;
            
            var tenantName = tenant?.FullName ?? "Tenant";
            var flatStr = apartment != null ? $" (Flat {apartment.FlatNumber})" : "";
            
            await _activityService.CreateActivity(
                "StatusChange",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Rent payment of {record.Amount:N0} received from {tenantName}{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue && oldStatus != IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"Payment for period '{record.Period}' is overdue. Amount: {record.Amount}.",
                cancellationToken);
        }
    }

    public async Task<byte[]> GenerateReceiptPdf(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var buildingName = "N/A";
        if (record.BuildingId.HasValue)
        {
            var b = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value);
            if (b != null) buildingName = b.BuildingName;
        }
        var tenantName = "N/A";
        if (record.TenantId.HasValue)
        {
            var t = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value);
            if (t != null) tenantName = t.FullName;
        }
        var flatNumber = "N/A";
        if (record.ApartmentId.HasValue)
        {
            var a = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value);
            if (a != null) flatNumber = a.FlatNumber;
        }

        // Minimal PDF format
        var pdfContent = 
            "%PDF-1.4\n" +
            "1 0 obj\n" +
            "<< /Type /Catalog /Pages 2 0 R >>\n" +
            "endobj\n" +
            "2 0 obj\n" +
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>\n" +
            "endobj\n" +
            "3 0 obj\n" +
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> /Contents 4 0 R >>\n" +
            "endobj\n" +
            "4 0 obj\n" +
            "<< /Length 1000 >>\n" +
            "stream\n" +
            "BT\n" +
            "/F1 18 Tf\n" +
            "70 700 Td\n" +
            "(ARDH PROPERTY MANAGEMENT - INCOME RECEIPT) Tj\n" +
            "0 -40 Td\n" +
            "/F1 12 Tf\n" +
            $"(Receipt ID: {record.Id}) Tj\n" +
            "0 -25 Td\n" +
            $"(Entity Type: {record.IncomeEntity}) Tj\n" +
            "0 -20 Td\n" +
            $"(Income Type: {record.IncomeType}) Tj\n" +
            "0 -20 Td\n" +
            $"(Amount: {record.Amount:F2} SAR) Tj\n" +
            "0 -20 Td\n" +
            $"(Tenant Name: {tenantName}) Tj\n" +
            "0 -20 Td\n" +
            $"(Building: {buildingName}) Tj\n" +
            "0 -20 Td\n" +
            $"(Apartment / Flat: {flatNumber}) Tj\n" +
            "0 -20 Td\n" +
            $"(Period: {record.Period}) Tj\n" +
            "0 -20 Td\n" +
            $"(Payment Date: {record.PaymentDate:yyyy-MM-dd}) Tj\n" +
            "0 -20 Td\n" +
            $"(Payment Method: {record.PaymentMethod}) Tj\n" +
            "0 -20 Td\n" +
            $"(Transaction Ref: {record.TransactionReference ?? "N/A"}) Tj\n" +
            "0 -20 Td\n" +
            $"(Status: {record.Status}) Tj\n" +
            "0 -25 Td\n" +
            $"(Notes: {record.Notes ?? "No additional notes."}) Tj\n" +
            "0 -40 Td\n" +
            "(/F1 10 Tf) Tj\n" +
            "(Thank you for your payment.) Tj\n" +
            "ET\n" +
            "endstream\n" +
            "endobj\n" +
            "xref\n" +
            "0 5\n" +
            "0000000000 65535 f \n" +
            "0000000009 00000 n \n" +
            "0000000058 00000 n \n" +
            "0000000115 00000 n \n" +
            "0000000282 00000 n \n" +
            "trailer\n" +
            "<< /Size 5 /Root 1 0 R >>\n" +
            "startxref\n" +
            "1350\n" +
            "%%EOF";

        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

    public async Task<byte[]> ExportToCsv(
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        Guid? tenantId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var tenantMap = tenants.ToDictionary(t => t.Id, t => t.FullName);

        var query = records.AsQueryable();

        if (incomeType.HasValue)
        {
            query = query.Where(x => x.IncomeType == incomeType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Period.ToLower().Contains(searchLower) ||
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (x.TransactionReference != null && x.TransactionReference.ToLower().Contains(searchLower)) ||
                (x.BuildingId.HasValue && buildingMap.ContainsKey(x.BuildingId.Value) && buildingMap[x.BuildingId.Value].ToLower().Contains(searchLower)) ||
                (x.TenantId.HasValue && tenantMap.ContainsKey(x.TenantId.Value) && tenantMap[x.TenantId.Value].ToLower().Contains(searchLower)) ||
                (x.ApartmentId.HasValue && apartmentMap.ContainsKey(x.ApartmentId.Value) && apartmentMap[x.ApartmentId.Value].ToLower().Contains(searchLower))
            );
        }

        var list = query.OrderByDescending(x => x.PaymentDate).ToList();

        var csv = new StringBuilder();
        csv.AppendLine("ID,IncomeEntity,IncomeType,Amount,TenantName,BuildingName,FlatNumber,Period,PaymentDate,PaymentMethod,TransactionReference,Status,Notes,CreatedAt");

        foreach (var r in list)
        {
            var tName = r.TenantId.HasValue && tenantMap.TryGetValue(r.TenantId.Value, out var tn) ? tn : "";
            var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : "";
            var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : "";

            csv.AppendLine($"\"{r.Id}\",\"{r.IncomeEntity}\",\"{r.IncomeType}\",{r.Amount:F2},\"{EscapeCsv(tName)}\",\"{EscapeCsv(bName)}\",\"{EscapeCsv(fNum)}\",\"{EscapeCsv(r.Period)}\",\"{r.PaymentDate:yyyy-MM-dd}\",\"{r.PaymentMethod}\",\"{EscapeCsv(r.TransactionReference)}\",\"{r.Status}\",\"{EscapeCsv(r.Notes)}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }
}
