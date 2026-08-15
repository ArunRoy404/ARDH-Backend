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
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

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
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (x.TransactionReference != null && x.TransactionReference.ToLower().Contains(searchLower)) ||
                (x.BuildingId.HasValue && buildingMap.ContainsKey(x.BuildingId.Value) && buildingMap[x.BuildingId.Value].ToLower().Contains(searchLower)) ||
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
            BuildingId = x.BuildingId,
            BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
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
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var currentTenant = apartment != null && apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;

        return new IncomeRecordViewModel
        {
            Id = record.Id,
            IncomeEntity = record.IncomeEntity,
            IncomeType = record.IncomeType,
            Amount = record.Amount,
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
        // Resolve the entity/type early for the duplicate + occupied checks below.
        var incomeEntity = request.IncomeEntity ?? IncomeEntity.ApartmentWise;
        var incomeType = request.IncomeType ?? IncomeType.Rent;
        var amount = request.Amount ?? 0;
        var paymentDate = request.PaymentDate ?? DateTime.UtcNow;
        var paymentMethod = request.PaymentMethod ?? IncomePaymentMethod.BankTransfer;
        var status = request.Status ?? IncomeStatus.Pending;

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist. Please choose a valid building.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId.Value);
            if (apartment == null)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist. Please choose a valid apartment.");
            }

            // Income entries are only allowed for apartments that are currently occupied.
            if (incomeEntity == IncomeEntity.ApartmentWise &&
                (apartment.CurrentTenantId == null || apartment.CurrentTenantId == Guid.Empty))
            {
                throw IncomeRecordException.BadRequestException(
                    $"Income entries can only be recorded for occupied apartments. Flat {apartment.FlatNumber} is currently vacant.");
            }
        }

        await EnsureNoDuplicate(apartmentId: request.ApartmentId, incomeType, amount, paymentDate, excludeId: null);

        var userId = _currentUser.GetCurrentUserId();

        var record = new IncomeRecord
        {
            Id = Guid.NewGuid(),
            IncomeEntity = incomeEntity,
            IncomeType = incomeType,
            Amount = amount,
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            PaymentDate = paymentDate,
            PaymentMethod = paymentMethod,
            TransactionReference = request.TransactionReference?.Trim(),
            Status = status,
            Notes = request.Notes?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.IncomeRecordRepository.AddAsync(record), cancellationToken);

        if (record.Status == IncomeStatus.Paid)
        {
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;

            var flatStr = apartment != null ? $" for Flat {apartment.FlatNumber}" : "";

            await _activityService.CreateActivity(
                "Create",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"A payment of {record.Amount:N0} ({record.IncomeType}) is overdue.",
                cancellationToken);
        }
    }

    public async Task Update(Guid id, IncomeRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var oldStatus = record.Status;

        var incomeEntity = request.IncomeEntity ?? record.IncomeEntity;
        var incomeType = request.IncomeType ?? record.IncomeType;
        var amount = request.Amount ?? record.Amount;
        var paymentDate = request.PaymentDate ?? record.PaymentDate;
        var paymentMethod = request.PaymentMethod ?? record.PaymentMethod;
        var status = request.Status ?? record.Status;

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist. Please choose a valid building.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId.Value);
            if (apartment == null)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist. Please choose a valid apartment.");
            }

            if (incomeEntity == IncomeEntity.ApartmentWise &&
                (apartment.CurrentTenantId == null || apartment.CurrentTenantId == Guid.Empty))
            {
                throw IncomeRecordException.BadRequestException(
                    $"Income entries can only be recorded for occupied apartments. Flat {apartment.FlatNumber} is currently vacant.");
            }
        }

        var duplicateSignatureChanged =
            request.ApartmentId != record.ApartmentId ||
            incomeType != record.IncomeType ||
            amount != record.Amount ||
            paymentDate.Date != record.PaymentDate.Date ||
            status != record.Status;

        if (duplicateSignatureChanged)
        {
            await EnsureNoDuplicate(request.ApartmentId, incomeType, amount, paymentDate, excludeId: id);
        }

        var userId = _currentUser.GetCurrentUserId();

        record.IncomeEntity = incomeEntity;
        record.IncomeType = incomeType;
        record.Amount = amount;
        record.BuildingId = request.BuildingId;
        record.ApartmentId = request.ApartmentId;
        record.PaymentDate = paymentDate;
        record.PaymentMethod = paymentMethod;
        record.TransactionReference = request.TransactionReference?.Trim();
        record.Status = status;
        record.Notes = request.Notes?.Trim();
        record.AttachmentUrl = request.AttachmentUrl?.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.IncomeRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == IncomeStatus.Paid && oldStatus != IncomeStatus.Paid)
        {
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;

            var flatStr = apartment != null ? $" for Flat {apartment.FlatNumber}" : "";

            await _activityService.CreateActivity(
                "Update",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue && oldStatus != IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"A payment of {record.Amount:N0} ({record.IncomeType}) is overdue.",
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
        var status = request.Status ?? record.Status;

        record.Status = status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.IncomeRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == IncomeStatus.Paid && oldStatus != IncomeStatus.Paid)
        {
            var apartment = record.ApartmentId.HasValue
                ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value)
                : null;

            var flatStr = apartment != null ? $" for Flat {apartment.FlatNumber}" : "";

            await _activityService.CreateActivity(
                "StatusChange",
                "IncomeRecord",
                record.Id,
                record.BuildingId,
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Received",
                $"Income of {record.Amount:N0} ({record.IncomeType}) received{flatStr}.",
                cancellationToken);
        }

        if (record.Status == IncomeStatus.Overdue && oldStatus != IncomeStatus.Overdue)
        {
            await _notificationService.CreateNotificationInternal(
                "finance",
                "Payment Overdue",
                $"A payment of {record.Amount:N0} ({record.IncomeType}) is overdue.",
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
            $"(Building: {buildingName}) Tj\n" +
            "0 -20 Td\n" +
            $"(Apartment / Flat: {flatNumber}) Tj\n" +
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

    public async Task<byte[]> ExportToXlsx(
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

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
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (x.TransactionReference != null && x.TransactionReference.ToLower().Contains(searchLower)) ||
                (x.BuildingId.HasValue && buildingMap.ContainsKey(x.BuildingId.Value) && buildingMap[x.BuildingId.Value].ToLower().Contains(searchLower)) ||
                (x.ApartmentId.HasValue && apartmentMap.ContainsKey(x.ApartmentId.Value) && apartmentMap[x.ApartmentId.Value].ToLower().Contains(searchLower))
            );
        }

        var list = query.OrderByDescending(x => x.PaymentDate).ToList();

        var rows = new List<List<string>>();
        var headers = new List<string> { "IncomeEntity", "IncomeType", "Amount", "BuildingName", "FlatNumber", "PaymentDate", "PaymentMethod", "TransactionReference", "Status", "Notes", "AttachmentUrl" };
        rows.Add(headers);

        foreach (var r in list)
        {
            rows.Add(new List<string>
            {
                r.IncomeEntity.ToString(),
                r.IncomeType.ToString(),
                r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var buildingName) ? buildingName : string.Empty,
                r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var flatNumber) ? flatNumber : string.Empty,
                r.PaymentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                r.PaymentMethod.ToString(),
                r.TransactionReference ?? string.Empty,
                r.Status.ToString(),
                r.Notes ?? string.Empty,
                r.AttachmentUrl ?? string.Empty
            });
        }

        return CleanArchitecture.Application.Common.Utilities.XlsxHelper.BuildXlsx(rows);
    }

    /// <summary>
    /// Rejects a duplicate income entry: the same apartment, income type, amount
    /// and payment month are not allowed more than once.
    /// </summary>
    private async Task EnsureNoDuplicate(
        Guid? apartmentId,
        IncomeType incomeType,
        decimal amount,
        DateTime paymentDate,
        Guid? excludeId)
    {
        if (!apartmentId.HasValue)
        {
            return;
        }

        var monthStart = new DateTime(paymentDate.Year, paymentDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var duplicate = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x =>
            x.Id != (excludeId ?? Guid.Empty) &&
            x.ApartmentId == apartmentId.Value &&
            x.IncomeType == incomeType &&
            x.Amount == amount &&
            x.PaymentDate >= monthStart &&
            x.PaymentDate < monthEnd);

        if (duplicate != null)
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == apartmentId.Value);
            var flat = apartment?.FlatNumber ?? "this apartment";

            throw IncomeRecordException.BadRequestException(
                $"A {incomeType} entry of {amount:N2} SAR for Flat {flat} already exists for {monthStart:MMMM yyyy}. Duplicate income entries are not allowed.");
        }
    }


}
