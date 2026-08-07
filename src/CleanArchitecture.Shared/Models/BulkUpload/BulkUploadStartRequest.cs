using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.BulkUpload;

public class BulkUploadStartRequest
{
    public BulkUploadModule Module { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}
