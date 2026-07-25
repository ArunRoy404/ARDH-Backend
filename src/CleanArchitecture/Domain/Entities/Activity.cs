using System;

namespace CleanArchitecture.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActionType { get; set; } = string.Empty; // e.g. Create, Update, Delete, StatusChange, MoveOut
    public string EntityType { get; set; } = string.Empty; // e.g. Building, Apartment, Tenant, MaintenanceRequest, IncomeRecord, ExpenseRecord
    public Guid EntityId { get; set; }
    public Guid? BuildingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Building? Building { get; set; }
}
