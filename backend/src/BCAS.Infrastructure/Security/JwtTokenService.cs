using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BCAS.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;

        if (_options.Key.Length < JwtOptions.MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {JwtOptions.MinimumKeyLength} characters long.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user)
    {
        var issuedAt = _clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = user.Id.ToString(CultureInfo.InvariantCulture),
                [JwtClaimNames.Email] = user.Email,
                [JwtClaimNames.Name] = user.FullName,
                [JwtClaimNames.Role] = user.RoleName,
                [JwtClaimNames.TokenId] = Guid.NewGuid().ToString("N")
            }
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }

    public (string Token, string TokenHash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Base64UrlEncoder.Encode(bytes);

        return (token, HashRefreshToken(token));
    }

    /// <summary>
    /// Only the hash is stored, so a dump of the RefreshTokens table cannot be replayed.
    /// </summary>
    public string HashRefreshToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string TokenId = "jti";
}
