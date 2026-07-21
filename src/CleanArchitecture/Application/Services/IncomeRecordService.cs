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

namespace CleanArchitecture.Application.Services;

public class IncomeRecordService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IIncomeRecordService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<IncomeRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        Guid? tenantId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.DeletedAt == null);

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
            CreatedBy = x.CreatedBy,
            UpdatedBy = x.UpdatedBy,
            DeletedBy = x.DeletedBy,
            RestoredBy = x.RestoredBy
        }).ToList();

        return new PaginatedList<IncomeRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<IncomeRecordViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value && x.DeletedAt == null) : null;
        var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value && x.DeletedAt == null) : null;
        var tenant = record.TenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value && x.DeletedAt == null) : null;

        return new IncomeRecordViewModel
        {
            Id = record.Id,
            IncomeEntity = record.IncomeEntity,
            IncomeType = record.IncomeType,
            Amount = record.Amount,
            TenantId = record.TenantId,
            TenantName = tenant != null ? tenant.FullName : null,
            BuildingId = record.BuildingId,
            BuildingName = building?.BuildingName,
            ApartmentId = record.ApartmentId,
            FlatNumber = apartment?.FlatNumber,
            Period = record.Period,
            PaymentDate = record.PaymentDate,
            PaymentMethod = record.PaymentMethod,
            TransactionReference = record.TransactionReference,
            Status = record.Status,
            Notes = record.Notes,
            AttachmentUrl = record.AttachmentUrl,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy,
            UpdatedBy = record.UpdatedBy,
            DeletedBy = record.DeletedBy,
            RestoredBy = record.RestoredBy
        };
    }

    public async Task Create(IncomeRecordCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value && x.DeletedAt == null);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await _unitOfWork.TenantRepository.AnyAsync(x => x.Id == request.TenantId.Value && x.DeletedAt == null);
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
    }

    public async Task Update(Guid id, IncomeRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value && x.DeletedAt == null);
            if (!buildingExists)
            {
                throw IncomeRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw IncomeRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await _unitOfWork.TenantRepository.AnyAsync(x => x.Id == request.TenantId.Value && x.DeletedAt == null);
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
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        record.DeletedAt = now;
        record.UpdatedAt = now;
        record.DeletedBy = userId;

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
    }

    public async Task UpdateStatus(Guid id, IncomeRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw IncomeRecordException.NotFoundException($"Income record with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();

        record.Status = request.Status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.IncomeRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> GenerateReceiptPdf(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
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
        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.DeletedAt == null);

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
