using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Models.User;

public class UpdateProfilePictureRequest
{
    [Required]
    [JsonPropertyName("avatarURL")]
    public string AvatarUrl { get; set; } = string.Empty;
}
