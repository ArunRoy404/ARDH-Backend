using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.BulkUpload;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IBulkUploadService
{
    /// <summary>Validates the request, creates a Processing record and enqueues the background job.</summary>
    Task<BulkUploadViewModel> StartAsync(BulkUploadStartRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a bulk upload job: parses the CSV, creates records, writes the processed CSV.</summary>
    Task ProcessAsync(Guid bulkUploadId, CancellationToken cancellationToken);

    /// <summary>Returns all bulk uploads, optionally filtered by module (newest first).</summary>
    Task<List<BulkUploadViewModel>> GetStatusAsync(BulkUploadModule? module, CancellationToken cancellationToken);

    /// <summary>Returns a single bulk upload by id.</summary>
    Task<BulkUploadViewModel> GetStatusByIdAsync(Guid bulkUploadId, CancellationToken cancellationToken);

    /// <summary>Builds a CSV template (headers + sample row) for the given module.</summary>
    Task<byte[]> GetTemplateAsync(BulkUploadModule module, CancellationToken cancellationToken);
}
