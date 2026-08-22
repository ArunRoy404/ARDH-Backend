using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.BulkUpload;

public class BulkUploadProgressEvent
{
    public Guid TrackId { get; set; }
    public BulkUploadModule Module { get; set; }
    public BulkUploadStatus Status { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ProcessedFileUrl { get; set; }
    public string? GlobalError { get; set; }
}
