using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Apartment;
using CleanArchitecture.Shared.Models.BulkUpload;
using CleanArchitecture.Shared.Models.Equipment;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Maintenance;
using CleanArchitecture.Shared.Models.Owner;
using CleanArchitecture.Shared.Models.Tenant;
using Microsoft.AspNetCore.Hosting;

namespace CleanArchitecture.Application.Services;

public class BulkUploadService(
    IUnitOfWork unitOfWork,
    IBulkUploadQueue bulkUploadQueue,
    ICurrentUser currentUser,
    AppSettings appSettings,
    IWebHostEnvironment environment,
    IApartmentService apartmentService,
    ITenantService tenantService,
    IOwnerService ownerService,
    IIncomeRecordService incomeRecordService,
    IExpenseRecordService expenseRecordService,
    IMaintenanceRequestService maintenanceRequestService,
    IEquipmentService equipmentService) : IBulkUploadService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IBulkUploadQueue _bulkUploadQueue = bulkUploadQueue;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly AppSettings _appSettings = appSettings;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly IApartmentService _apartmentService = apartmentService;
    private readonly ITenantService _tenantService = tenantService;
    private readonly IOwnerService _ownerService = ownerService;
    private readonly IIncomeRecordService _incomeRecordService = incomeRecordService;
    private readonly IExpenseRecordService _expenseRecordService = expenseRecordService;
    private readonly IMaintenanceRequestService _maintenanceRequestService = maintenanceRequestService;
    private readonly IEquipmentService _equipmentService = equipmentService;

    // ─────────────────────────────────────────────────────────────────────────
    // Start / enqueue
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<BulkUploadViewModel> StartAsync(BulkUploadStartRequest request, CancellationToken cancellationToken)
    {
        var moduleName = ToModuleString(request.Module);
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw BulkUploadException.BadRequestException(
                $"Unsupported module '{request.Module}'. Valid modules: apartments, tenants, owners, income, expenses, maintenance, equipment.");
        }

        if (string.IsNullOrWhiteSpace(request.FileUrl))
        {
            throw BulkUploadException.BadRequestException("FileUrl is required. Upload the CSV file first (POST /api/upload/csv) and pass the returned URL.");
        }

        var record = new BulkUpload
        {
            Id = Guid.NewGuid(),
            Module = moduleName,
            Status = BulkUploadStatus.Processing.ToString(),
            OriginalFileUrl = request.FileUrl.Trim(),
            TotalCount = 0,
            SuccessCount = 0,
            FailedCount = 0,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.BulkUploadRepository.AddAsync(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _bulkUploadQueue.Enqueue(record.Id);

        return ToViewModel(record);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Background processing
    // ─────────────────────────────────────────────────────────────────────────

    public async Task ProcessAsync(Guid bulkUploadId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.BulkUploadRepository.FirstOrDefaultAsync(x => x.Id == bulkUploadId);
        if (record == null)
        {
            return;
        }

        record.Status = BulkUploadStatus.Processing.ToString();
        record.StartedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.BulkUploadRepository.Update(record); // entity loaded AsNoTracking -> re-attach
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (headers, rows) = ReadCsv(record.OriginalFileUrl);
            var headerIndex = CsvHelper.BuildHeaderIndex(headers);

            // Global check: mandatory columns must be present
            var missing = RequiredColumns(record.Module)
                .Where(h => !headerIndex.ContainsKey(h))
                .ToList();

            if (missing.Count > 0)
            {
                record.GlobalError =
                    $"Missing required column(s): {string.Join(", ", missing)}. Download the template for '{record.Module}' to see the exact column names.";
                record.Status = BulkUploadStatus.Failed.ToString();
                record.FinishedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BulkUploadRepository.Update(record);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var processed = new List<List<string>> { headers.Concat(new[] { "status", "error" }).ToList() };

            var total = 0;
            var success = 0;
            var failed = 0;

            // Skip the header row
            foreach (var row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                total++;

                var (request, errors) = ParseRow(record.Module, row, headerIndex);

                if (request == null || errors.Count > 0)
                {
                    failed++;
                    processed.Add(row.Concat(new[] { "Failed", string.Join("; ", errors) }).ToList());
                    continue;
                }

                try
                {
                    await CreateRecord(record.Module, request, cancellationToken);
                    success++;
                    processed.Add(row.Concat(new[] { "Success", string.Empty }).ToList());
                }
                catch (Exception ex)
                {
                    failed++;
                    processed.Add(row.Concat(new[] { "Failed", ToReadableError(ex) }).ToList());
                }
            }

            var processedUrl = await WriteProcessedCsvAsync(record.Id, processed, cancellationToken);

            record.TotalCount = total;
            record.SuccessCount = success;
            record.FailedCount = failed;
            record.ProcessedFileUrl = processedUrl;
            record.Status = BulkUploadStatus.Finished.ToString();
            record.FinishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            record.GlobalError = ToReadableError(ex);
            record.Status = BulkUploadStatus.Failed.ToString();
            record.FinishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Status queries
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<BulkUploadViewModel>> GetStatusAsync(BulkUploadModule? module, CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.BulkUploadRepository.GetAllAsync();
        var query = records.AsQueryable();

        if (module.HasValue)
        {
            var moduleName = ToModuleString(module.Value);
            query = query.Where(x => x.Module.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(x => x.CreatedAt)
            .Select(ToViewModel)
            .ToList();
    }

    public async Task<BulkUploadViewModel> GetStatusByIdAsync(Guid bulkUploadId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.BulkUploadRepository.FirstOrDefaultAsync(x => x.Id == bulkUploadId)
            ?? throw BulkUploadException.NotFoundException($"Bulk upload with ID '{bulkUploadId}' was not found.");

        return ToViewModel(record);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Templates
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<byte[]> GetTemplateAsync(BulkUploadModule module, CancellationToken cancellationToken)
    {
        var moduleName = ToModuleString(module);
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw BulkUploadException.BadRequestException($"Unsupported module '{module}'.");
        }

        var csv = TemplateCsv(moduleName);
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CSV reading / writing
    // ─────────────────────────────────────────────────────────────────────────

    private (List<string> headers, List<List<string>> rows) ReadCsv(string fileUrl)
    {
        var localPath = ResolveLocalFilePath(fileUrl);
        if (!File.Exists(localPath))
        {
            throw BulkUploadException.BadRequestException(
                $"Uploaded CSV file could not be found on the server ('{localPath}'). Please upload the file again via POST /api/upload/csv.");
        }

        var text = File.ReadAllText(localPath);
        var rows = CsvHelper.Parse(text);

        if (rows.Count < 2)
        {
            throw BulkUploadException.BadRequestException(
                "The CSV file must contain a header row followed by at least one data row.");
        }

        return (rows[0], rows);
    }

    private async Task<string> WriteProcessedCsvAsync(Guid bulkUploadId, List<List<string>> rows, CancellationToken cancellationToken)
    {
        var csvText = CsvHelper.BuildCsv(rows);
        var fileName = $"bulk_processed_{bulkUploadId}.csv";
        var storagePath = GetStoragePath();
        var filePath = Path.Combine(storagePath, fileName);

        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        await File.WriteAllTextAsync(filePath, csvText, cancellationToken);

        return BuildFileUrl(fileName);
    }

    private string ResolveLocalFilePath(string fileUrl)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
        return Path.Combine(GetStoragePath(), fileName);
    }

    private string GetStoragePath()
    {
        return Path.Combine(_environment.ContentRootPath, _appSettings.FileStorageSettings.Path);
    }

    private string BuildFileUrl(string fileName)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_appSettings.BaseURL)
            ? _appSettings.BaseURL.TrimEnd('/')
            : _appSettings.AppUrl?.TrimEnd('/') ?? string.Empty;
        var folderName = _appSettings.FileStorageSettings.Path.Trim('/');
        return $"{baseUrl}/{folderName}/{fileName}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-module: required columns + row parsing + create dispatch
    // ─────────────────────────────────────────────────────────────────────────

    private static string[] RequiredColumns(string module) => module.ToLowerInvariant() switch
    {
        "apartments" => new[] { "buildingid", "ownerid", "nestawayid", "flatnumber", "apartmenttype" },
        "tenants" => new[] { "buildingid", "apartmentid", "fullname", "phone", "idtype", "idnumber", "moveindate", "leasestartdate", "monthlyrent" },
        "owners" => new[] { "fullname", "phone", "email", "idtype", "idnumber", "bankname", "accountnumber", "ifsccode" },
        "income" => new[] { "incomeentity", "incometype", "amount", "paymentdate", "paymentmethod", "status" },
        "expenses" => new[] { "category", "expensehead", "specificitem", "nature", "amount", "entity", "buildingid", "expensedate", "paymentmethod", "status" },
        "maintenance" => new[] { "title", "description", "category", "priority", "buildingid", "status", "estimatedcost", "scheduleddate" },
        "equipment" => new[] { "buildingid", "name", "type", "brand", "installdate", "status" },
        _ => Array.Empty<string>()
    };

    private (object? request, List<string> errors) ParseRow(string module, List<string> row, Dictionary<string, int> headerIndex)
        => module.ToLowerInvariant() switch
        {
            "apartments" => ParseApartment(row, headerIndex),
            "tenants" => ParseTenant(row, headerIndex),
            "owners" => ParseOwner(row, headerIndex),
            "income" => ParseIncome(row, headerIndex),
            "expenses" => ParseExpense(row, headerIndex),
            "maintenance" => ParseMaintenance(row, headerIndex),
            "equipment" => ParseEquipment(row, headerIndex),
            _ => (null, new List<string> { "Unsupported module." })
        };

    private Task CreateRecord(string module, object request, CancellationToken cancellationToken)
        => module.ToLowerInvariant() switch
        {
            "apartments" => _apartmentService.Create((ApartmentCreateRequest)request, cancellationToken),
            "tenants" => _tenantService.Create((TenantCreateRequest)request, cancellationToken),
            "owners" => _ownerService.Create((OwnerCreateRequest)request, cancellationToken),
            "income" => _incomeRecordService.Create((IncomeRecordCreateRequest)request, cancellationToken),
            "expenses" => _expenseRecordService.Create((ExpenseRecordCreateRequest)request, cancellationToken),
            "maintenance" => _maintenanceRequestService.Create((MaintenanceRequestCreateRequest)request, cancellationToken),
            "equipment" => _equipmentService.Create((EquipmentCreateRequest)request, cancellationToken),
            _ => throw BulkUploadException.BadRequestException("Unsupported module.")
        };

    // ── Apartments ──────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseApartment(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new ApartmentCreateRequest
        {
            BuildingId = GetGuid(row, h, "BuildingId", "BuildingId", errors),
            OwnerId = GetGuid(row, h, "OwnerId", "OwnerId", errors),
            NestawayId = GetRequired(row, h, "NestawayId", "NestawayId", errors),
            FlatNumber = GetRequired(row, h, "FlatNumber", "FlatNumber", errors),
            ApartmentType = GetRequired(row, h, "ApartmentType", "ApartmentType", errors),
            Floor = GetInt(row, h, "Floor", "Floor", 0, errors) ?? 0,
            AreaSqft = GetDecimal(row, h, "AreaSqft", "AreaSqft", errors),
            Bedrooms = GetInt(row, h, "Bedrooms", "Bedrooms", errors),
            Bathrooms = GetInt(row, h, "Bathrooms", "Bathrooms", errors),
            HasBalcony = GetBool(row, h, "HasBalcony", "HasBalcony", false, errors),
            ParkingSlot = GetOptional(row, h, "ParkingSlot"),
            ExpectedRent = GetDecimal(row, h, "ExpectedRent", "ExpectedRent", 0m, errors),
            MaintenanceCharge = GetDecimal(row, h, "MaintenanceCharge", "MaintenanceCharge", 0m, errors),
            WaterCharge = GetDecimal(row, h, "WaterCharge", "WaterCharge", 0m, errors),
            Notes = GetOptional(row, h, "Notes")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Tenants ─────────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseTenant(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new TenantCreateRequest
        {
            BuildingId = GetGuid(row, h, "BuildingId", "BuildingId", errors),
            ApartmentId = GetGuid(row, h, "ApartmentId", "ApartmentId", errors),
            FullName = GetRequired(row, h, "FullName", "FullName", errors),
            Phone = GetRequired(row, h, "Phone", "Phone", errors),
            Email = GetOptional(row, h, "Email"),
            IdType = GetEnum<OwnerIdType>(row, h, "IdType", "IdType", errors) ?? OwnerIdType.Aadhar,
            IdNumber = GetRequired(row, h, "IdNumber", "IdNumber", errors),
            IdProofAttachmentUrl = GetOptional(row, h, "IdProofAttachmentUrl"),
            MoveInDate = GetDate(row, h, "MoveInDate", "MoveInDate", errors) ?? DateTime.UtcNow,
            LeaseStartDate = GetDate(row, h, "LeaseStartDate", "LeaseStartDate", errors) ?? DateTime.UtcNow,
            LeaseEndDate = GetDate(row, h, "LeaseEndDate", "LeaseEndDate", errors),
            MonthlyRent = GetDecimal(row, h, "MonthlyRent", "MonthlyRent", 0m, errors),
            SecurityDeposit = GetDecimal(row, h, "SecurityDeposit", "SecurityDeposit", errors),
            EmergencyContactName = GetOptional(row, h, "EmergencyContactName"),
            EmergencyContactPhone = GetOptional(row, h, "EmergencyContactPhone"),
            Status = GetEnum<TenantStatus>(row, h, "Status", "Status", errors),
            Notes = GetOptional(row, h, "Notes")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Owners ──────────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseOwner(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new OwnerCreateRequest
        {
            FullName = GetRequired(row, h, "FullName", "FullName", errors),
            Phone = GetRequired(row, h, "Phone", "Phone", errors),
            Email = GetRequired(row, h, "Email", "Email", errors),
            City = GetOptional(row, h, "City"),
            Address = GetOptional(row, h, "Address"),
            IdType = GetEnum<OwnerIdType>(row, h, "IdType", "IdType", errors) ?? OwnerIdType.Aadhar,
            IdNumber = GetRequired(row, h, "IdNumber", "IdNumber", errors),
            BankName = GetRequired(row, h, "BankName", "BankName", errors),
            AccountNumber = GetRequired(row, h, "AccountNumber", "AccountNumber", errors),
            IfscCode = GetRequired(row, h, "IfscCode", "IfscCode", errors),
            Status = GetEnum<OwnerStatus>(row, h, "Status", "Status", errors) ?? OwnerStatus.Active,
            Notes = GetOptional(row, h, "Notes")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Income ──────────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseIncome(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var entity = GetEnum<IncomeEntity>(row, h, "IncomeEntity", "IncomeEntity", errors);

        var req = new IncomeRecordCreateRequest
        {
            IncomeEntity = entity,
            IncomeType = GetEnum<IncomeType>(row, h, "IncomeType", "IncomeType", errors),
            Amount = GetDecimal(row, h, "Amount", "Amount", errors),
            BuildingId = GetNullableGuid(row, h, "BuildingId"),
            ApartmentId = GetNullableGuid(row, h, "ApartmentId"),
            PaymentDate = GetDate(row, h, "PaymentDate", "PaymentDate", errors),
            PaymentMethod = GetEnum<IncomePaymentMethod>(row, h, "PaymentMethod", "PaymentMethod", errors),
            TransactionReference = GetOptional(row, h, "TransactionReference"),
            Status = GetEnum<IncomeStatus>(row, h, "Status", "Status", errors),
            Notes = GetOptional(row, h, "Notes"),
            AttachmentUrl = GetOptional(row, h, "AttachmentUrl")
        };

        if (entity == IncomeEntity.ApartmentWise)
        {
            if (!req.BuildingId.HasValue)
            {
                errors.Add("BuildingId is required when IncomeEntity is ApartmentWise.");
            }
            if (!req.ApartmentId.HasValue)
            {
                errors.Add("ApartmentId is required when IncomeEntity is ApartmentWise.");
            }
        }

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Expenses ────────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseExpense(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new ExpenseRecordCreateRequest
        {
            Category = GetEnum<ExpenseCategory>(row, h, "Category", "Category", errors) ?? ExpenseCategory.Utility,
            ExpenseHead = GetRequired(row, h, "ExpenseHead", "ExpenseHead", errors),
            SpecificItem = GetRequired(row, h, "SpecificItem", "SpecificItem", errors),
            VendorId = GetNullableGuid(row, h, "VendorId"),
            Nature = GetEnum<ExpenseNature>(row, h, "Nature", "Nature", errors),
            Amount = GetDecimal(row, h, "Amount", "Amount", errors),
            Entity = GetEnum<ExpenseEntity>(row, h, "Entity", "Entity", errors),
            BuildingId = GetNullableGuid(row, h, "BuildingId"),
            ApartmentId = GetNullableGuid(row, h, "ApartmentId"),
            ExpenseDate = GetDate(row, h, "ExpenseDate", "ExpenseDate", errors),
            PaymentMethod = GetRequired(row, h, "PaymentMethod", "PaymentMethod", errors),
            Status = GetEnum<ExpenseStatus>(row, h, "Status", "Status", errors),
            Reference = GetOptional(row, h, "Reference"),
            AttachmentUrl = GetOptional(row, h, "AttachmentUrl"),
            Description = GetOptional(row, h, "Description"),
            TankerNumber = GetOptional(row, h, "TankerNumber"),
            TimeOfDelivery = GetDate(row, h, "TimeOfDelivery", "TimeOfDelivery", errors),
            DeliveryDriverName = GetOptional(row, h, "DeliveryDriverName"),
            ManagerInAttendance = GetOptional(row, h, "ManagerInAttendance"),
            LitersFilled = GetInt(row, h, "LitersFilled", "LitersFilled", errors)
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Maintenance ─────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseMaintenance(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new MaintenanceRequestCreateRequest
        {
            Title = GetRequired(row, h, "Title", "Title", errors),
            Description = GetRequired(row, h, "Description", "Description", errors),
            Category = GetRequired(row, h, "Category", "Category", errors),
            Priority = GetEnum<MaintenancePriority>(row, h, "Priority", "Priority", errors),
            BuildingId = GetGuid(row, h, "BuildingId", "BuildingId", errors),
            ApartmentId = GetNullableGuid(row, h, "ApartmentId"),
            VendorId = GetNullableGuid(row, h, "VendorId"),
            EquipmentId = GetNullableGuid(row, h, "EquipmentId"),
            Status = GetEnum<MaintenanceStatus>(row, h, "Status", "Status", errors),
            EstimatedCost = GetDecimal(row, h, "EstimatedCost", "EstimatedCost", errors),
            AnnualCost = GetDecimal(row, h, "AnnualCost", "AnnualCost", 0m, errors),
            ScheduledDate = GetDate(row, h, "ScheduledDate", "ScheduledDate", errors),
            StartDate = GetDate(row, h, "StartDate", "StartDate", errors),
            RecurrenceFrequency = GetEnum<MaintenanceRecurrenceFrequency>(row, h, "RecurrenceFrequency", "RecurrenceFrequency", errors),
            ReceiptAttachmentUrl = GetOptional(row, h, "ReceiptAttachmentUrl"),
            Notes = GetOptional(row, h, "Notes")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Equipment ───────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseEquipment(List<string> row, Dictionary<string, int> h)
    {
        var errors = new List<string>();
        var req = new EquipmentCreateRequest
        {
            BuildingId = GetGuid(row, h, "BuildingId", "BuildingId", errors),
            Name = GetRequired(row, h, "Name", "Name", errors),
            Type = GetRequired(row, h, "Type", "Type", errors),
            Brand = GetRequired(row, h, "Brand", "Brand", errors),
            Model = GetOptional(row, h, "Model"),
            SerialNumber = GetOptional(row, h, "SerialNumber"),
            InstallDate = GetDate(row, h, "InstallDate", "InstallDate", errors) ?? DateTime.UtcNow,
            WarrantyExpiryDate = GetDate(row, h, "WarrantyExpiryDate", "WarrantyExpiryDate", errors),
            Status = GetRequired(row, h, "Status", "Status", errors) ?? "Operational",
            Notes = GetOptional(row, h, "Notes"),
            AttachmentUrl = GetOptional(row, h, "AttachmentUrl")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Typed cell readers
    // ─────────────────────────────────────────────────────────────────────────

    private static string? GetOptional(List<string> row, Dictionary<string, int> h, string header) => CsvHelper.GetValue(row, h, header);

    private static string GetRequired(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} is required.");
        }
        return value ?? string.Empty;
    }

    private static Guid GetGuid(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} is required.");
            return Guid.Empty;
        }
        if (!Guid.TryParse(value, out var guid))
        {
            errors.Add($"{label} '{value}' is not a valid ID.");
            return Guid.Empty;
        }
        return guid;
    }

    private static Guid? GetNullableGuid(List<string> row, Dictionary<string, int> h, string header)
    {
        var value = CsvHelper.GetValue(row, h, header);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static decimal GetDecimal(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} is required.");
            return 0m;
        }
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} '{value}' is not a valid number.");
            return 0m;
        }
        return result;
    }

    private static decimal GetDecimal(List<string> row, Dictionary<string, int> h, string header, string label, decimal defaultValue, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} '{value}' is not a valid number.");
            return defaultValue;
        }
        return result;
    }

    private static int GetInt(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} '{value}' is not a valid whole number.");
            return 0;
        }
        return result;
    }

    private static int? GetInt(List<string> row, Dictionary<string, int> h, string header, string label, int defaultValue, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} '{value}' is not a valid whole number.");
            return defaultValue;
        }
        return result;
    }

    private static bool GetBool(List<string> row, Dictionary<string, int> h, string header, string label, bool defaultValue, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        if (value is "1" or "0")
        {
            return value == "1";
        }
        errors.Add($"{label} '{value}' is not a valid boolean. Use true/false or 1/0.");
        return defaultValue;
    }

    private static DateTime? GetDate(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }
        errors.Add($"{label} '{value}' is not a valid date. Use YYYY-MM-DD (e.g. 2026-08-07).");
        return null;
    }

    private static T? GetEnum<T>(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors) where T : struct, Enum
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }
        errors.Add($"{label} '{value}' is invalid. Valid values: {string.Join(", ", Enum.GetNames<T>())}.");
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Module helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string ToModuleString(BulkUploadModule module) => module switch
    {
        BulkUploadModule.Apartments => "apartments",
        BulkUploadModule.Tenants => "tenants",
        BulkUploadModule.Owners => "owners",
        BulkUploadModule.Income => "income",
        BulkUploadModule.Expenses => "expenses",
        BulkUploadModule.Maintenance => "maintenance",
        BulkUploadModule.Equipment => "equipment",
        _ => string.Empty
    };

    private static BulkUploadModule? ToModuleEnum(string module) => module.ToLowerInvariant() switch
    {
        "apartments" => BulkUploadModule.Apartments,
        "tenants" => BulkUploadModule.Tenants,
        "owners" => BulkUploadModule.Owners,
        "income" => BulkUploadModule.Income,
        "expenses" => BulkUploadModule.Expenses,
        "maintenance" => BulkUploadModule.Maintenance,
        "equipment" => BulkUploadModule.Equipment,
        _ => null
    };

    private static BulkUploadViewModel ToViewModel(BulkUpload bulkUpload)
    {
        return new BulkUploadViewModel
        {
            Id = bulkUpload.Id,
            Module = ToModuleEnum(bulkUpload.Module) ?? BulkUploadModule.Apartments,
            Status = Enum.TryParse<BulkUploadStatus>(bulkUpload.Status, true, out var status) ? status : BulkUploadStatus.Processing,
            OriginalFileUrl = bulkUpload.OriginalFileUrl,
            ProcessedFileUrl = bulkUpload.ProcessedFileUrl,
            TotalCount = bulkUpload.TotalCount,
            SuccessCount = bulkUpload.SuccessCount,
            FailedCount = bulkUpload.FailedCount,
            GlobalError = bulkUpload.GlobalError,
            StartedAt = bulkUpload.StartedAt,
            FinishedAt = bulkUpload.FinishedAt,
            CreatedAt = bulkUpload.CreatedAt,
            CreatedBy = bulkUpload.CreatedBy
        };
    }

    private static string ToReadableError(Exception ex)
    {
        if (ex is UserFriendlyException friendly)
        {
            return friendly.Message;
        }
        if (ex.InnerException is UserFriendlyException innerFriendly)
        {
            return innerFriendly.Message;
        }
        return "An unexpected error occurred while processing this row. Please check the values and try again.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Templates (headers + sample rows matching the create POST API exactly)
    // ─────────────────────────────────────────────────────────────────────────

    private static string TemplateCsv(string module) => module.ToLowerInvariant() switch
    {
        "apartments" => BuildTemplate(
            new[] { "BuildingId", "OwnerId", "NestawayId", "FlatNumber", "Floor", "ApartmentType", "AreaSqft", "Bedrooms", "Bathrooms", "HasBalcony", "ParkingSlot", "ExpectedRent", "MaintenanceCharge", "WaterCharge", "Notes" },
            new[] { "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "f1a3b822-29c4-52a8-ad29-c8be5d491f24", "NST-BULK-001", "999", "9", "3 BHK", "1100", "3", "2", "true", "P-99", "45000", "3000", "500", "Bulk upload sample apartment" }),

        "tenants" => BuildTemplate(
            new[] { "BuildingId", "ApartmentId", "FullName", "Phone", "Email", "IdType", "IdNumber", "IdProofAttachmentUrl", "MoveInDate", "LeaseStartDate", "LeaseEndDate", "MonthlyRent", "SecurityDeposit", "EmergencyContactName", "EmergencyContactPhone", "Status", "Notes" },
            new[] { "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "a5c7b822-29c4-52a8-ad29-c8be5d491f32", "Bulk Test Tenant", "+91 9000000000", "bulk.tenant@example.com", "Aadhar", "TENANT-BULK-001", "", "2026-09-01", "2026-09-01", "2027-08-31", "45000", "90000", "Emergency Contact", "+91 9000000001", "Active", "Bulk upload sample tenant" }),

        "owners" => BuildTemplate(
            new[] { "FullName", "Phone", "Email", "City", "Address", "IdType", "IdNumber", "BankName", "AccountNumber", "IfscCode", "Status", "Notes" },
            new[] { "Bulk Test Owner", "+91 9000000002", "bulk.owner@example.com", "Mumbai", "1, Test Street", "Aadhar", "OWNER-BULK-001", "HDFC Bank", "88888888888", "HDFC0000999", "Active", "Bulk upload sample owner" }),

        "income" => BuildTemplate(
            new[] { "IncomeEntity", "IncomeType", "Amount", "BuildingId", "ApartmentId", "PaymentDate", "PaymentMethod", "TransactionReference", "Status", "Notes", "AttachmentUrl" },
            new[] { "ApartmentWise", "Rent", "45000", "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "a1f3b822-29c4-52a8-ad29-c8be5d491f24", "2026-09-05", "BankTransfer", "TRX-BULK-001", "Paid", "Bulk upload sample income", "" }),

        "expenses" => BuildTemplate(
            new[] { "Category", "ExpenseHead", "SpecificItem", "VendorId", "Nature", "Amount", "Entity", "BuildingId", "ApartmentId", "ExpenseDate", "PaymentMethod", "Status", "Reference", "AttachmentUrl", "Description", "TankerNumber", "TimeOfDelivery", "DeliveryDriverName", "ManagerInAttendance", "LitersFilled" },
            new[] { "Utility", "Electricity", "Monthly bill", "f7c7b822-29c4-52a8-ad29-c8be5d491f41", "Service", "1250.50", "General", "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "", "2026-08-07", "BankTransfer", "Paid", "REF-BULK-001", "", "Bulk upload sample expense", "", "", "", "", "" }),

        "maintenance" => BuildTemplate(
            new[] { "Title", "Description", "Category", "Priority", "BuildingId", "ApartmentId", "VendorId", "EquipmentId", "Status", "EstimatedCost", "AnnualCost", "ScheduledDate", "StartDate", "RecurrenceFrequency", "ReceiptAttachmentUrl", "Notes" },
            new[] { "Bulk test request", "Bulk upload sample maintenance request", "Plumbing", "High", "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "a1f3b822-29c4-52a8-ad29-c8be5d491f24", "f7a3b822-29c4-52a8-ad29-c8be5d491f24", "e1a3b822-29c4-52a8-ad29-c8be5d491f24", "Open", "1500", "18000", "2026-08-10", "", "Monthly", "", "Bulk upload sample maintenance" }),

        "equipment" => BuildTemplate(
            new[] { "BuildingId", "Name", "Type", "Brand", "Model", "SerialNumber", "InstallDate", "WarrantyExpiryDate", "Status", "Notes", "AttachmentUrl" },
            new[] { "b1f7b822-29c4-52a8-ad29-c8be5d491f24", "Bulk Test Pump", "Pump", "Kirloskar", "KM-40", "SN-BULK-001", "2026-01-01", "2028-01-01", "Operational", "Bulk upload sample equipment", "" }),

        _ => string.Empty
    };

    private static string BuildTemplate(string[] headers, string[] sampleRow)
    {
        var rows = new List<List<string>>
        {
            headers.ToList(),
            sampleRow.ToList()
        };
        return CsvHelper.BuildCsv(rows);
    }
}
