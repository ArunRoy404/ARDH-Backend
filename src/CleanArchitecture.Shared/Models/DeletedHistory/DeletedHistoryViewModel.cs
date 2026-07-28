using System;

namespace CleanArchitecture.Shared.Models.DeletedHistory;

public class DeletedHistoryViewModel
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityTitle { get; set; } = string.Empty;
    public string? DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoredBy { get; set; }
    public bool PermanentlyDeletable { get; set; }
    public bool Restorable { get; set; }
}
 