using System;

namespace CleanArchitecture.Shared.Models.Tenant;

public class TenantMoveOutCreateRequest
{
    public DateTime MoveOutDate { get; set; }
    public decimal PendingRent { get; set; }
    public decimal DamageAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string IdNumber { get; set; } = null!;
    public string? HandoverNote { get; set; }
}
