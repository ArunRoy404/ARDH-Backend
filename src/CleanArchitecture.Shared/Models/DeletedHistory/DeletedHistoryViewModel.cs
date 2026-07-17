using System;

namespace CleanArchitecture.Shared.Models.DeletedHistory;

public class DeletedHistoryViewModel
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityTitle { get; set; } = string.Empty;
    public Guid? DeletedBy { get; set; }
    public string? DeletedByName { get; set; }
    public DateTime DeletedAt { get; set; }
    public DateTime? RestoredAt { get; set; }
    public Guid? RestoredBy { get; set; }
    public string? RestoredByName { get; set; }
}
