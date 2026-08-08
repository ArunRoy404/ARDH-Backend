using System;

namespace CleanArchitecture.Domain.Entities;

public class BulkUpload
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing"; // Processing | Finished | Failed
    public string OriginalFileUrl { get; set; } = string.Empty;
    public string? ProcessedFileUrl { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? GlobalError { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
