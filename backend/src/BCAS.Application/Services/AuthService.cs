using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BCAS.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        // The same message is returned for an unknown address and a bad password so the
        // endpoint cannot be used to enumerate accounts.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            throw new AuthenticationFailedException("Invalid email address or password.");
        }

        if (!user.IsActive)
        {
            throw new AuthenticationFailedException("This account has been deactivated.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (stored is null || !stored.IsActive(_clock.UtcNow))
        {
            throw new AuthenticationFailedException("The refresh token is invalid or has expired.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("The refresh token is invalid or has expired.");
        }

        // Rotate: the presented token is retired as soon as a new pair is handed out.
        await _refreshTokens.RevokeAsync(stored.Id, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            await _refreshTokens.RevokeAsync(stored.Id, cancellationToken);
        }
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return ToResponse(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.CreateAccessToken(user);
        var (refreshToken, refreshTokenHash) = _tokenService.CreateRefreshToken();

        await _refreshTokens.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = _clock.UtcNow,
                ExpiresAt = _clock.UtcNow.Add(_tokenService.RefreshTokenLifetime)
            },
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Value,
            RefreshToken = refreshToken,
            ExpiresAt = accessToken.ExpiresAtUtc,
            User = ToResponse(user)
        };
    }

    private static CurrentUserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.RoleName
    };
}
