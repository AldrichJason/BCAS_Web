using System.ComponentModel.DataAnnotations;

namespace BCAS.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum length for the HMAC-SHA256 signing key (256 bits).</summary>
    public const int MinimumKeyLength = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(MinimumKeyLength)]
    public string Key { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
