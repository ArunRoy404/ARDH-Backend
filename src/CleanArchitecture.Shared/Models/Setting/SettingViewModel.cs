using System;

namespace CleanArchitecture.Shared.Models.Setting;

public class SettingViewModel
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Fav { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public Guid? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
