using System;

namespace CleanArchitecture.Domain.Entities;

public class DeletedHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityTitle { get; set; } = string.Empty;
    public Guid? DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RestoredAt { get; set; }
    public Guid? RestoredBy { get; set; }
}
