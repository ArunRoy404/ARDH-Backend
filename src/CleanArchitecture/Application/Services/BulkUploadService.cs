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
using Microsoft.Extensions.Logging;

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
    IEquipmentService equipmentService,
    IBulkUploadProgressBroadcaster progressBroadcaster,
    ILogger<BulkUploadService> logger) : IBulkUploadService
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
    private readonly IBulkUploadProgressBroadcaster _progressBroadcaster = progressBroadcaster;
    private readonly ILogger<BulkUploadService> _logger = logger;

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
            throw BulkUploadException.BadRequestException("FileUrl is required. Upload the XLSX file first (POST /api/upload/xlsx) and pass the returned URL.");
        }

        var fileUrl = request.FileUrl.Trim();
        var matches = System.Text.RegularExpressions.Regex.Matches(fileUrl, @"https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (matches.Count > 1)
        {
            fileUrl = fileUrl.Substring(0, matches[1].Index);
        }

        var record = new BulkUpload
        {
            Id = Guid.NewGuid(),
            Module = moduleName,
            Status = BulkUploadStatus.Processing.ToString(),
            OriginalFileUrl = fileUrl,
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

    public async Task<List<Guid>> GetInterruptedJobIdsAsync(CancellationToken cancellationToken)
    {
        var stuck = await _unitOfWork.BulkUploadRepository.GetAllAsync(
            x => x.Status == BulkUploadStatus.Processing.ToString() && !x.FinishedAt.HasValue);
        return stuck.Select(x => x.Id).ToList();
    }

    public async Task ProcessAsync(Guid bulkUploadId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.BulkUploadRepository.FirstOrDefaultAsync(x => x.Id == bulkUploadId);
        if (record == null)
        {
            return;
        }

        // The background job runs in its own DI scope with no HttpContext, so ICurrentUser can't
        // resolve a user from claims. Since ICurrentUser is scoped and shared by every service
        // resolved in this job's scope, overriding it here makes every record created below
        // (Apartment/Tenant/Owner/etc.) attribute CreatedBy to the user who started the upload.
        if (record.CreatedBy.HasValue && record.CreatedBy.Value != Guid.Empty)
        {
            _currentUser.SetCurrentUserId(record.CreatedBy.Value);
        }

        record.Status = BulkUploadStatus.Processing.ToString();
        record.StartedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.BulkUploadRepository.Update(record); // entity loaded AsNoTracking -> re-attach
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        List<string> headers = null;
        List<List<string>> rows = null;

        try
        {
            (headers, rows) = ReadXlsx(record.OriginalFileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk upload {BulkUploadId} ({Module}) failed while reading the uploaded file.", record.Id, record.Module);
            record.GlobalError = ToReadableError(ex);
            record.Status = BulkUploadStatus.Failed.ToString();
            record.FinishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var headerIndex = CsvHelper.BuildHeaderIndex(headers);

            // Global check: mandatory columns must be present
            var missing = RequiredColumns(record.Module)
                .Where(h => !headerIndex.ContainsKey(h))
                .ToList();

            if (missing.Count > 0)
            {
                var globalError = $"Missing required column(s): {string.Join(", ", missing)}. Download the template for '{record.Module}' to see the exact column names.";
                record.GlobalError = globalError;
                record.Status = BulkUploadStatus.Failed.ToString();

                var processed = new List<List<string>> { headers.Concat(new[] { "status", "error" }).ToList() };
                var total = 0;
                foreach (var row in rows.Skip(1))
                {
                    if (row.All(string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }
                    total++;
                    processed.Add(PadAndAppendStatus(row, headers.Count, "Failed", globalError));
                }

                record.TotalCount = total;
                record.SuccessCount = 0;
                record.FailedCount = total;
                record.ProcessedFileUrl = await WriteProcessedXlsxAsync(record.Id, processed, cancellationToken);
                record.FinishedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BulkUploadRepository.Update(record);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var moduleEnum = ToModuleEnum(record.Module) ?? BulkUploadModule.Apartments;

            // Preload name->ID lookups once per job so rows can reference buildings,
            // owners and apartments by name/number instead of GUIDs.
            var lookup = await BuildLookupAsync(cancellationToken);

            var processedList = new List<List<string>> { headers.Concat(new[] { "status", "error" }).ToList() };

            var validRows = rows.Skip(1).Where(r => !r.All(string.IsNullOrWhiteSpace)).ToList();
            var totalRows = validRows.Count;
            var success = 0;
            var failed = 0;
            var processedCount = 0;

            record.TotalCount = totalRows;
            record.ProgressPercentage = 0;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _progressBroadcaster.PublishProgress(new BulkUploadProgressEvent
            {
                TrackId = record.Id,
                Module = moduleEnum,
                Status = BulkUploadStatus.Processing,
                TotalCount = totalRows,
                ProcessedCount = 0,
                SuccessCount = 0,
                FailedCount = 0,
                ProgressPercentage = 0
            });

            var rowNumber = 1; // header is row 1, so data rows start at 2
            foreach (var row in validRows)
            {
                rowNumber++;
                var (request, errors) = ParseRow(record.Module, row, headerIndex, lookup);

                if (request == null || errors.Count > 0)
                {
                    failed++;
                    processedList.Add(PadAndAppendStatus(row, headers.Count, "Failed", string.Join("; ", errors)));
                }
                else
                {
                    try
                    {
                        await CreateRecord(record.Module, request, lookup, cancellationToken);
                        success++;
                        processedList.Add(PadAndAppendStatus(row, headers.Count, "Success", string.Empty));
                    }
                    catch (Exception ex)
                    {
                        // Log the full exception (not the row's raw field values - those can hold
                        // PII like phone/ID/bank numbers) so the real cause is recoverable from
                        // server logs even when ToReadableError falls back to a generic message.
                        _logger.LogError(ex, "Bulk upload {BulkUploadId} ({Module}) row {RowNumber} failed.", record.Id, record.Module, rowNumber);
                        _unitOfWork.ClearChangeTracker();
                        failed++;
                        processedList.Add(PadAndAppendStatus(row, headers.Count, "Failed", ToReadableError(ex)));
                    }
                }

                processedCount = success + failed;
                var pct = totalRows > 0 ? (int)Math.Round((double)processedCount / totalRows * 100) : 100;
                record.ProgressPercentage = Math.Min(pct, 99); // 100% is reserved for Finished state
                record.SuccessCount = success;
                record.FailedCount = failed;

                _progressBroadcaster.PublishProgress(new BulkUploadProgressEvent
                {
                    TrackId = record.Id,
                    Module = moduleEnum,
                    Status = BulkUploadStatus.Processing,
                    TotalCount = totalRows,
                    ProcessedCount = processedCount,
                    SuccessCount = success,
                    FailedCount = failed,
                    ProgressPercentage = record.ProgressPercentage
                });
            }

            var processedUrl = await WriteProcessedXlsxAsync(record.Id, processedList, cancellationToken);

            record.TotalCount = totalRows;
            record.SuccessCount = success;
            record.FailedCount = failed;
            record.ProgressPercentage = 100;
            record.ProcessedFileUrl = processedUrl;
            record.Status = BulkUploadStatus.Finished.ToString();
            record.FinishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _progressBroadcaster.PublishProgress(new BulkUploadProgressEvent
            {
                TrackId = record.Id,
                Module = moduleEnum,
                Status = BulkUploadStatus.Finished,
                TotalCount = totalRows,
                ProcessedCount = totalRows,
                SuccessCount = success,
                FailedCount = failed,
                ProgressPercentage = 100,
                ProcessedFileUrl = processedUrl
            });
        }
        catch (Exception ex)
        {
            var moduleEnum = ToModuleEnum(record.Module) ?? BulkUploadModule.Apartments;
            _logger.LogError(ex, "Bulk upload {BulkUploadId} ({Module}) failed.", record.Id, record.Module);
            record.GlobalError = ToReadableError(ex);
            record.Status = BulkUploadStatus.Failed.ToString();
            record.FinishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BulkUploadRepository.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _progressBroadcaster.PublishProgress(new BulkUploadProgressEvent
            {
                TrackId = record.Id,
                Module = moduleEnum,
                Status = BulkUploadStatus.Failed,
                TotalCount = record.TotalCount,
                ProcessedCount = record.SuccessCount + record.FailedCount,
                SuccessCount = record.SuccessCount,
                FailedCount = record.FailedCount,
                ProgressPercentage = record.ProgressPercentage,
                GlobalError = record.GlobalError
            });
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

        return await Task.FromResult(XlsxHelper.BuildXlsx(TemplateRows(moduleName), "Template"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // XLSX reading / writing
    // ─────────────────────────────────────────────────────────────────────────

    private (List<string> headers, List<List<string>> rows) ReadXlsx(string fileUrl)
    {
        var localPath = ResolveLocalFilePath(fileUrl);
        if (!File.Exists(localPath))
        {
            throw BulkUploadException.BadRequestException(
                $"Uploaded XLSX file could not be found on the server ('{localPath}'). Please upload the file again via POST /api/upload/xlsx.");
        }

        using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
        var rows = XlsxHelper.ReadRows(stream);

        if (rows.Count < 2)
        {
            throw BulkUploadException.BadRequestException(
                "The XLSX file must contain a header row followed by at least one data row.");
        }

        return (rows[0], rows);
    }

    private async Task<string> WriteProcessedXlsxAsync(Guid bulkUploadId, List<List<string>> rows, CancellationToken cancellationToken)
    {
        var xlsxBytes = XlsxHelper.BuildXlsx(rows, "Processed");
        var fileName = $"bulk_processed_{bulkUploadId}.xlsx";
        var bulkFolder = (_appSettings.FileStorageSettings.BulkUploadPath ?? "bulk-upload").Trim('/');
        var storagePath = Path.Combine(_environment.ContentRootPath, bulkFolder);
        var filePath = Path.Combine(storagePath, fileName);

        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        await File.WriteAllBytesAsync(filePath, xlsxBytes, cancellationToken);

        var baseUrl = !string.IsNullOrWhiteSpace(_appSettings.BaseURL)
            ? _appSettings.BaseURL.TrimEnd('/')
            : _appSettings.AppUrl?.TrimEnd('/') ?? string.Empty;

        return $"{baseUrl}/{bulkFolder}/{fileName}";
    }

    private string ResolveLocalFilePath(string fileUrl)
    {
        string relativePath;
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            relativePath = uri.LocalPath.TrimStart('/');
        }
        else
        {
            relativePath = fileUrl.TrimStart('/');
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, relativePath);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        var fileName = Path.GetFileName(relativePath);
        var bulkFolder = (_appSettings.FileStorageSettings.BulkUploadPath ?? "bulk-upload").Trim('/');
        var fallbackBulkPath = Path.Combine(_environment.ContentRootPath, bulkFolder, fileName);
        if (File.Exists(fallbackBulkPath))
        {
            return fallbackBulkPath;
        }

        var imageFolder = (_appSettings.FileStorageSettings.ImagePath ?? "image").Trim('/');
        var fallbackImagePath = Path.Combine(_environment.ContentRootPath, imageFolder, fileName);
        if (File.Exists(fallbackImagePath))
        {
            return fallbackImagePath;
        }

        return fallbackBulkPath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-module: required columns + row parsing + create dispatch
    // ─────────────────────────────────────────────────────────────────────────

    private static string[] RequiredColumns(string module) => module.ToLowerInvariant() switch
    {
        "apartments" => new[] { "buildingname", "ownername", "nestawayid", "flatnumber", "apartmenttype" },
        "tenants" => new[] { "buildingname", "flatnumber", "fullname", "phone", "idtype", "idnumber", "moveindate", "leasestartdate", "monthlyrent" },
        "owners" => new[] { "fullname", "phone", "email", "idtype", "idnumber", "bankname", "accountnumber", "ifsccode" },
        "income" => new[] { "incomeentity", "incometype", "amount", "paymentdate", "paymentmethod", "status" },
        "expenses" => new[] { "category", "expensehead", "specificitem", "nature", "amount", "entity", "buildingname", "expensedate", "paymentmethod", "status" },
        "maintenance" => new[] { "title", "description", "category", "priority", "buildingname", "status", "estimatedcost", "scheduleddate" },
        "equipment" => new[] { "buildingname", "name", "type", "brand", "installdate", "status" },
        _ => Array.Empty<string>()
    };

    private static (object? request, List<string> errors) ParseRow(string module, List<string> row, Dictionary<string, int> headerIndex, BulkLookup lookup)
        => module.ToLowerInvariant() switch
        {
            "apartments" => ParseApartment(row, headerIndex, lookup),
            "tenants" => ParseTenant(row, headerIndex, lookup),
            "owners" => ParseOwner(row, headerIndex),
            "income" => ParseIncome(row, headerIndex, lookup),
            "expenses" => ParseExpense(row, headerIndex, lookup),
            "maintenance" => ParseMaintenance(row, headerIndex, lookup),
            "equipment" => ParseEquipment(row, headerIndex, lookup),
            _ => (null, new List<string> { "Unsupported module." })
        };

    private async Task CreateRecord(string module, object request, BulkLookup lookup, CancellationToken cancellationToken)
    {
        switch (module.ToLowerInvariant())
        {
            case "apartments":
                var aptReq = (ApartmentCreateRequest)request;
                await _apartmentService.Create(aptReq, cancellationToken);
                var createdApt = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(a => a.BuildingId == aptReq.BuildingId && a.FlatNumber == aptReq.FlatNumber);
                if (createdApt != null)
                {
                    lookup.Apartments[(aptReq.BuildingId, NormalizeName(aptReq.FlatNumber))] = createdApt.Id;
                }
                break;

            case "tenants":
                await _tenantService.Create((TenantCreateRequest)request, cancellationToken);
                break;

            case "owners":
                var ownerReq = (OwnerCreateRequest)request;
                await _ownerService.Create(ownerReq, cancellationToken);
                var createdOwner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(o => o.FullName == ownerReq.FullName);
                if (createdOwner != null)
                {
                    lookup.Owners[NormalizeName(ownerReq.FullName)] = createdOwner.Id;
                }
                break;

            case "income":
                await _incomeRecordService.Create((IncomeRecordCreateRequest)request, cancellationToken);
                break;

            case "expenses":
                await _expenseRecordService.Create((ExpenseRecordCreateRequest)request, cancellationToken);
                break;

            case "maintenance":
                await _maintenanceRequestService.Create((MaintenanceRequestCreateRequest)request, cancellationToken);
                break;

            case "equipment":
                var eqReq = (EquipmentCreateRequest)request;
                await _equipmentService.Create(eqReq, cancellationToken);
                var createdEq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(e => e.BuildingId == eqReq.BuildingId && e.Name == eqReq.Name);
                if (createdEq != null)
                {
                    lookup.Equipment[NormalizeName(eqReq.Name)] = createdEq.Id;
                }
                break;

            default:
                throw BulkUploadException.BadRequestException("Unsupported module.");
        }
    }

    // ── Apartments ──────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseApartment(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var req = new ApartmentCreateRequest
        {
            BuildingId = ResolveBuilding(row, h, lookup, errors),
            OwnerId = ResolveOwner(row, h, lookup, errors),
            NestawayId = GetRequired(row, h, "NestawayId", "NestawayId", errors),
            FlatNumber = GetRequired(row, h, "FlatNumber", "FlatNumber", errors),
            ApartmentType = GetRequired(row, h, "ApartmentType", "ApartmentType", errors),
            Floor = GetInt(row, h, "Floor", "Floor", 0, errors) ?? 0,
            AreaSqft = GetOptionalDecimal(row, h, "AreaSqft", "AreaSqft", errors),
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

    private static (object?, List<string>) ParseTenant(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var req = new TenantCreateRequest
        {
            BuildingId = ResolveBuilding(row, h, lookup, errors),
            ApartmentId = ResolveApartment(row, h, lookup, errors),
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
            SecurityDeposit = GetOptionalDecimal(row, h, "SecurityDeposit", "SecurityDeposit", errors),
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

    private static (object?, List<string>) ParseIncome(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var entity = GetEnum<IncomeEntity>(row, h, "IncomeEntity", "IncomeEntity", errors);

        Guid? buildingId = null;
        Guid? apartmentId = null;
        if (entity == IncomeEntity.ApartmentWise)
        {
            buildingId = ResolveBuilding(row, h, lookup, errors);
            apartmentId = ResolveApartment(row, h, lookup, errors);
        }

        var req = new IncomeRecordCreateRequest
        {
            IncomeEntity = entity,
            IncomeType = GetEnum<IncomeType>(row, h, "IncomeType", "IncomeType", errors),
            Amount = GetDecimal(row, h, "Amount", "Amount", errors),
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            PaymentDate = GetDate(row, h, "PaymentDate", "PaymentDate", errors),
            PaymentMethod = GetEnum<IncomePaymentMethod>(row, h, "PaymentMethod", "PaymentMethod", errors),
            TransactionReference = GetOptional(row, h, "TransactionReference"),
            Status = GetEnum<IncomeStatus>(row, h, "Status", "Status", errors),
            Notes = GetOptional(row, h, "Notes"),
            AttachmentUrl = GetOptional(row, h, "AttachmentUrl")
        };

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Expenses ────────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseExpense(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var req = new ExpenseRecordCreateRequest
        {
            Category = GetEnum<ExpenseCategory>(row, h, "Category", "Category", errors) ?? ExpenseCategory.Utility,
            ExpenseHead = GetRequired(row, h, "ExpenseHead", "ExpenseHead", errors),
            SpecificItem = GetRequired(row, h, "SpecificItem", "SpecificItem", errors),
            VendorId = ResolveOptionalVendor(row, h, lookup, errors),
            Nature = GetEnum<ExpenseNature>(row, h, "Nature", "Nature", errors),
            Amount = GetDecimal(row, h, "Amount", "Amount", errors),
            Entity = GetEnum<ExpenseEntity>(row, h, "Entity", "Entity", errors),
            BuildingId = ResolveBuilding(row, h, lookup, errors),
            ApartmentId = ResolveOptionalApartment(row, h, lookup, errors),
            ExpenseDate = GetDate(row, h, "ExpenseDate", "ExpenseDate", errors),
            PaymentMethod = GetRequired(row, h, "PaymentMethod", "PaymentMethod", errors),
            Status = GetEnum<ExpenseStatus>(row, h, "Status", "Status", errors),
            Reference = GetOptional(row, h, "Reference"),
            AttachmentUrl = GetOptional(row, h, "AttachmentUrl"),
            Description = GetOptional(row, h, "Description"),
            TankerNumber = GetOptional(row, h, "TankerNumber"),
            TimeOfDelivery = GetOptional(row, h, "TimeOfDelivery"),
            DeliveryDriverName = GetOptional(row, h, "DeliveryDriverName"),
            ManagerInAttendance = GetOptional(row, h, "ManagerInAttendance"),
            LitersFilled = GetInt(row, h, "LitersFilled", "LitersFilled", errors)
        };

        if (req.Entity == ExpenseEntity.ApartmentSpecific && !req.ApartmentId.HasValue)
        {
            errors.Add("FlatNumber is required when Entity is ApartmentSpecific.");
        }

        return (errors.Count > 0 ? null : req, errors);
    }

    // ── Maintenance ─────────────────────────────────────────────────────────

    private static (object?, List<string>) ParseMaintenance(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var req = new MaintenanceRequestCreateRequest
        {
            Title = GetRequired(row, h, "Title", "Title", errors),
            Description = GetRequired(row, h, "Description", "Description", errors),
            Category = GetRequired(row, h, "Category", "Category", errors),
            Priority = GetEnum<MaintenancePriority>(row, h, "Priority", "Priority", errors),
            BuildingId = ResolveBuilding(row, h, lookup, errors),
            ApartmentId = ResolveOptionalApartment(row, h, lookup, errors),
            VendorId = ResolveOptionalVendor(row, h, lookup, errors),
            EquipmentId = ResolveOptionalEquipment(row, h, lookup, errors),
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

    private static (object?, List<string>) ParseEquipment(List<string> row, Dictionary<string, int> h, BulkLookup lookup)
    {
        var errors = new List<string>();
        var req = new EquipmentCreateRequest
        {
            BuildingId = ResolveBuilding(row, h, lookup, errors),
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
    // Name -> ID lookups (bulk uploads reference records by name/number, not GUIDs)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class BulkLookup
    {
        /// <summary>Building name (trimmed, case-insensitive) -> building id.</summary>
        public Dictionary<string, Guid> Buildings { get; }

        /// <summary>Owner full name (trimmed, case-insensitive) -> owner id.</summary>
        public Dictionary<string, Guid> Owners { get; }

        /// <summary>(building id, flat number) -> apartment id. Flat numbers are unique per building.</summary>
        public Dictionary<(Guid BuildingId, string FlatNumber), Guid> Apartments { get; }

        /// <summary>Vendor name (trimmed, case-insensitive) -> vendor id.</summary>
        public Dictionary<string, Guid> Vendors { get; }

        /// <summary>Equipment name (trimmed, case-insensitive) -> equipment id.</summary>
        public Dictionary<string, Guid> Equipment { get; }

        public BulkLookup(
            Dictionary<string, Guid> buildings,
            Dictionary<string, Guid> owners,
            Dictionary<(Guid, string), Guid> apartments,
            Dictionary<string, Guid> vendors,
            Dictionary<string, Guid> equipment)
        {
            Buildings = buildings;
            Owners = owners;
            Apartments = apartments;
            Vendors = vendors;
            Equipment = equipment;
        }
    }

    private async Task<BulkLookup> BuildLookupAsync(CancellationToken cancellationToken)
    {
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var equipment = await _unitOfWork.EquipmentRepository.GetAllAsync();

        var buildingMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in buildings)
        {
            var key = NormalizeName(b.BuildingName);
            if (!string.IsNullOrEmpty(key) && !buildingMap.ContainsKey(key))
            {
                buildingMap[key] = b.Id;
            }
        }

        var ownerMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in owners)
        {
            var key = NormalizeName(o.FullName);
            if (!string.IsNullOrEmpty(key) && !ownerMap.ContainsKey(key))
            {
                ownerMap[key] = o.Id;
            }
        }

        var apartmentMap = new Dictionary<(Guid, string), Guid>();
        foreach (var a in apartments)
        {
            var flatKey = NormalizeName(a.FlatNumber);
            if (!string.IsNullOrEmpty(flatKey))
            {
                apartmentMap[(a.BuildingId, flatKey)] = a.Id;
            }
        }

        var vendorMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in vendors)
        {
            var key = NormalizeName(v.Name);
            if (!string.IsNullOrEmpty(key) && !vendorMap.ContainsKey(key))
            {
                vendorMap[key] = v.Id;
            }
        }

        var equipmentMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in equipment)
        {
            var key = NormalizeName(e.Name);
            if (!string.IsNullOrEmpty(key) && !equipmentMap.ContainsKey(key))
            {
                equipmentMap[key] = e.Id;
            }
        }

        return new BulkLookup(buildingMap, ownerMap, apartmentMap, vendorMap, equipmentMap);
    }

    private static string NormalizeName(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static Guid ResolveBuilding(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        var name = CsvHelper.GetValue(row, h, "BuildingName");
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("BuildingName is required.");
            return Guid.Empty;
        }
        if (lookup.Buildings.TryGetValue(NormalizeName(name), out var id))
        {
            return id;
        }
        errors.Add($"Building '{name}' was not found. The building must be created before it can be referenced.");
        return Guid.Empty;
    }

    private static Guid ResolveOwner(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        var name = CsvHelper.GetValue(row, h, "OwnerName");
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("OwnerName is required.");
            return Guid.Empty;
        }
        if (lookup.Owners.TryGetValue(NormalizeName(name), out var id))
        {
            return id;
        }
        errors.Add($"Owner '{name}' was not found. The owner must be created before it can be referenced.");
        return Guid.Empty;
    }

    private static Guid ResolveApartment(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        var buildingName = CsvHelper.GetValue(row, h, "BuildingName");
        var flatNumber = CsvHelper.GetValue(row, h, "FlatNumber");
        if (string.IsNullOrWhiteSpace(buildingName) || string.IsNullOrWhiteSpace(flatNumber))
        {
            errors.Add("BuildingName and FlatNumber are required to identify the apartment.");
            return Guid.Empty;
        }
        if (!lookup.Buildings.TryGetValue(NormalizeName(buildingName), out var buildingId))
        {
            errors.Add($"Building '{buildingName}' was not found. The building must be created before it can be referenced.");
            return Guid.Empty;
        }
        if (lookup.Apartments.TryGetValue((buildingId, NormalizeName(flatNumber)), out var apartmentId))
        {
            return apartmentId;
        }
        errors.Add($"Apartment with flat number '{flatNumber}' was not found in building '{buildingName}'.");
        return Guid.Empty;
    }

    private static Guid? ResolveOptionalApartment(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        if (CsvHelper.GetValue(row, h, "FlatNumber") is not { } flatNumber || string.IsNullOrWhiteSpace(flatNumber))
        {
            return null;
        }
        return ResolveApartment(row, h, lookup, errors);
    }

    private static Guid? ResolveOptionalVendor(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        var name = CsvHelper.GetValue(row, h, "VendorName");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        if (lookup.Vendors.TryGetValue(NormalizeName(name), out var id))
        {
            return id;
        }
        errors.Add($"Vendor '{name}' was not found. The vendor must be created before it can be referenced.");
        return null;
    }

    private static Guid? ResolveOptionalEquipment(List<string> row, Dictionary<string, int> h, BulkLookup lookup, List<string> errors)
    {
        var name = CsvHelper.GetValue(row, h, "EquipmentName");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        if (lookup.Equipment.TryGetValue(NormalizeName(name), out var id))
        {
            return id;
        }
        errors.Add($"Equipment '{name}' was not found. The equipment must be created before it can be referenced.");
        return null;
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

    /// <summary>
    /// Reads an optional decimal column: blank cells yield null (no error), matching the
    /// create-API semantics where the field is optional (e.g. Apartment.AreaSqft,
    /// Tenant.SecurityDeposit). Invalid non-blank values still produce a row error.
    /// </summary>
    private static decimal? GetOptionalDecimal(List<string> row, Dictionary<string, int> h, string header, string label, List<string> errors)
    {
        var value = CsvHelper.GetValue(row, h, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} '{value}' is not a valid number.");
            return null;
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
        var originalFileUrl = bulkUpload.OriginalFileUrl;
        var matchesOriginal = System.Text.RegularExpressions.Regex.Matches(originalFileUrl, @"https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (matchesOriginal.Count > 1)
        {
            originalFileUrl = originalFileUrl.Substring(0, matchesOriginal[1].Index);
        }

        var processedFileUrl = bulkUpload.ProcessedFileUrl;
        if (!string.IsNullOrEmpty(processedFileUrl))
        {
            var matchesProcessed = System.Text.RegularExpressions.Regex.Matches(processedFileUrl, @"https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchesProcessed.Count > 1)
            {
                processedFileUrl = processedFileUrl.Substring(0, matchesProcessed[1].Index);
            }
        }

        return new BulkUploadViewModel
        {
            Id = bulkUpload.Id,
            Module = ToModuleEnum(bulkUpload.Module) ?? BulkUploadModule.Apartments,
            Status = Enum.TryParse<BulkUploadStatus>(bulkUpload.Status, true, out var status) ? status : BulkUploadStatus.Processing,
            OriginalFileUrl = originalFileUrl,
            ProcessedFileUrl = processedFileUrl,
            TotalCount = bulkUpload.TotalCount,
            SuccessCount = bulkUpload.SuccessCount,
            FailedCount = bulkUpload.FailedCount,
            ProgressPercentage = bulkUpload.ProgressPercentage,
            GlobalError = bulkUpload.GlobalError,
            StartedAt = bulkUpload.StartedAt,
            FinishedAt = bulkUpload.FinishedAt,
            CreatedAt = bulkUpload.CreatedAt,
            CreatedBy = bulkUpload.CreatedBy
        };
    }

    private static string ToReadableError(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current is UserFriendlyException friendly)
            {
                return friendly.Message;
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    if (inner is UserFriendlyException innerFriendly)
                    {
                        return innerFriendly.Message;
                    }
                }
            }

            current = current.InnerException;
        }

        return "An unexpected error occurred while processing this row. Please check the values and try again.";
    }

    private static List<string> PadAndAppendStatus(List<string> row, int targetHeaderCount, string status, string error)
    {
        var padded = new List<string>(row);
        while (padded.Count < targetHeaderCount)
        {
            padded.Add(string.Empty);
        }
        padded.Add(status);
        padded.Add(error);
        return padded;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Templates (headers + sample rows matching the create POST API exactly)
    // ─────────────────────────────────────────────────────────────────────────

    private static List<List<string>> TemplateRows(string module) => module.ToLowerInvariant() switch
    {
        "apartments" => BuildTemplate(
            new[] { "BuildingName", "OwnerName", "NestawayId", "FlatNumber", "Floor", "ApartmentType", "AreaSqft", "Bedrooms", "Bathrooms", "HasBalcony", "ParkingSlot", "ExpectedRent", "MaintenanceCharge", "WaterCharge", "Notes" },
            new[] { "Grand Plaza Towers", "Rahul Verma", "NEST-BULK-001", "999", "9", "3 BHK", "1100", "3", "2", "true", "P-99", "45000", "3000", "500", "Bulk upload sample apartment" }),

        "tenants" => BuildTemplate(
            new[] { "BuildingName", "FlatNumber", "FullName", "Phone", "Email", "IdType", "IdNumber", "IdProofAttachmentUrl", "MoveInDate", "LeaseStartDate", "LeaseEndDate", "MonthlyRent", "SecurityDeposit", "EmergencyContactName", "EmergencyContactPhone", "Status", "Notes" },
            new[] { "Grand Plaza Towers", "302", "Bulk Test Tenant", "+91 9000000000", "bulk.tenant@example.com", "Aadhar", "TENANT-BULK-001", "", "2026-09-01", "2026-09-01", "2027-08-31", "45000", "90000", "Emergency Contact", "+91 9000000001", "Active", "Bulk upload sample tenant" }),

        "owners" => BuildTemplate(
            new[] { "FullName", "Phone", "Email", "City", "Address", "IdType", "IdNumber", "BankName", "AccountNumber", "IfscCode", "Status", "Notes" },
            new[] { "Bulk Test Owner", "+91 9000000002", "bulk.owner@example.com", "Mumbai", "1, Test Street", "Aadhar", "OWNER-BULK-001", "HDFC Bank", "88888888888", "HDFC0000999", "Active", "Bulk upload sample owner" }),

        "income" => BuildTemplate(
            new[] { "IncomeEntity", "IncomeType", "Amount", "BuildingName", "FlatNumber", "PaymentDate", "PaymentMethod", "TransactionReference", "Status", "Notes", "AttachmentUrl" },
            new[] { "ApartmentWise", "Rent", "45000", "Grand Plaza Towers", "302", "2026-09-05", "BankTransfer", "TRX-BULK-001", "Paid", "Bulk upload sample income", "" }),

        "expenses" => BuildTemplate(
            new[] { "Category", "ExpenseHead", "SpecificItem", "VendorName", "Nature", "Amount", "Entity", "BuildingName", "FlatNumber", "ExpenseDate", "PaymentMethod", "Status", "Reference", "AttachmentUrl", "Description", "TankerNumber", "TimeOfDelivery", "DeliveryDriverName", "ManagerInAttendance", "LitersFilled" },
            new[] { "Utility", "Electricity", "Monthly bill", "BESCOM", "Service", "1250.50", "General", "Grand Plaza Towers", "", "2026-08-07", "BankTransfer", "Paid", "REF-BULK-001", "", "Bulk upload sample expense", "", "", "", "", "" }),

        "maintenance" => BuildTemplate(
            new[] { "Title", "Description", "Category", "Priority", "BuildingName", "FlatNumber", "VendorName", "EquipmentName", "Status", "EstimatedCost", "AnnualCost", "ScheduledDate", "StartDate", "RecurrenceFrequency", "ReceiptAttachmentUrl", "Notes" },
            new[] { "Bulk test request", "Bulk upload sample maintenance request", "Plumbing", "High", "Grand Plaza Towers", "302", "Sunil Kumar", "Water Pump Block A", "Open", "1500", "18000", "2026-08-10", "", "Monthly", "", "Bulk upload sample maintenance" }),

        "equipment" => BuildTemplate(
            new[] { "BuildingName", "Name", "Type", "Brand", "Model", "SerialNumber", "InstallDate", "WarrantyExpiryDate", "Status", "Notes", "AttachmentUrl" },
            new[] { "Grand Plaza Towers", "Bulk Test Pump", "Pump", "Kirloskar", "KM-40", "SN-BULK-001", "2026-01-01", "2028-01-01", "Operational", "Bulk upload sample equipment", "" }),

        _ => new List<List<string>>()
    };

    private static List<List<string>> BuildTemplate(string[] headers, string[] sampleRow)
    {
        return new List<List<string>>
        {
            headers.ToList(),
            sampleRow.ToList()
        };
    }
}
