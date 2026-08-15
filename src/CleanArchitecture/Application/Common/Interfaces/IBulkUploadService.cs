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

    /// <summary>Processes a bulk upload job: parses the XLSX, creates records, writes the processed XLSX.</summary>
    Task ProcessAsync(Guid bulkUploadId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ids of bulk-upload jobs that are stuck in "Processing" (no FinishedAt) —
    /// e.g. the server restarted or crashed mid-job. Used by the background service on startup
    /// to re-queue them so they finish instead of staying stuck forever.
    /// </summary>
    Task<List<Guid>> GetInterruptedJobIdsAsync(CancellationToken cancellationToken);

    /// <summary>Returns all bulk uploads, optionally filtered by module (newest first).</summary>
    Task<List<BulkUploadViewModel>> GetStatusAsync(BulkUploadModule? module, CancellationToken cancellationToken);

    /// <summary>Returns a single bulk upload by id.</summary>
    Task<BulkUploadViewModel> GetStatusByIdAsync(Guid bulkUploadId, CancellationToken cancellationToken);

    /// <summary>Builds an XLSX template (headers + sample row) for the given module.</summary>
    Task<byte[]> GetTemplateAsync(BulkUploadModule module, CancellationToken cancellationToken);
}
