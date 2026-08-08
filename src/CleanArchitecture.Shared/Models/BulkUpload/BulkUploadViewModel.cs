using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.BulkUpload;

public class BulkUploadViewModel
{
    public Guid Id { get; set; }
    public BulkUploadModule Module { get; set; }
    public BulkUploadStatus Status { get; set; }
    public string OriginalFileUrl { get; set; } = string.Empty;
    public string? ProcessedFileUrl { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? GlobalError { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
