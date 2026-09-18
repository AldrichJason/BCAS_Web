using BCAS.Domain.Entities;
using BCAS.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace BCAS.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void A_hashed_password_verifies_against_the_original()
    {
        var hash = _hasher.Hash("S3cure-Passw0rd");

        Assert.True(_hasher.Verify("S3cure-Passw0rd", hash));
        Assert.False(_hasher.Verify("something-else", hash));
    }

    [Fact]
    public void A_malformed_hash_is_rejected_instead_of_throwing()
    {
        Assert.False(_hasher.Verify("S3cure-Passw0rd", "not-a-bcrypt-hash"));
        Assert.False(_hasher.Verify("S3cure-Passw0rd", string.Empty));
    }
}

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "bcas-web-api",
        Audience = "bcas-web-client",
        Key = "unit-test-signing-key-that-is-long-enough",
        AccessTokenMinutes = 30,
        RefreshTokenDays = 7
    };

    private readonly JwtTokenService _service = new(
        Microsoft.Extensions.Options.Options.Create(Options),
        new FixedClock(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public void CreateAccessToken_returns_a_signed_jwt_that_expires_as_configured()
    {
        var user = new User { Id = 7, Email = "admin@bcas.edu.ph", FullName = "Portal Admin", RoleName = RoleNames.Administrator };

        var token = _service.CreateAccessToken(user);

        Assert.Equal(3, token.Value.Split('.').Length);
        Assert.Equal(new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc), token.ExpiresAtUtc);
    }

    [Fact]
    public void Refresh_tokens_are_random_and_stored_only_as_a_hash()
    {
        var (first, firstHash) = _service.CreateRefreshToken();
        var (second, _) = _service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.NotEqual(first, firstHash);
        Assert.Equal(64, firstHash.Length);
        Assert.Equal(firstHash, _service.HashRefreshToken(first));
    }

    [Fact]
    public void A_short_signing_key_is_rejected_at_startup()
    {
        var weak = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "i",
            Audience = "a",
            Key = "too-short"
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = new JwtTokenService(weak, new FixedClock(DateTime.UtcNow));
        });
    }
}
