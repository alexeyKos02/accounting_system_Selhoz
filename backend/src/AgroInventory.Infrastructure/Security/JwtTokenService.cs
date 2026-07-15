using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Выпуск JWT access-токенов (HS256) и refresh-токенов (ТЗ §1). Refresh — 256 бит случайности,
/// в БД хранится только SHA-256 хэш.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions _options;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(AuthOptions options, TimeProvider clock)
    {
        _options = options;
        _clock = clock;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(User user)
    {
        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaimNames.Subject, user.Id.ToString()),
            new(JwtClaimNames.IsSystemAdmin, user.IsSystemAdmin ? "true" : "false"),
            new(JwtClaimNames.Name, user.DisplayName),
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtClaimNames.Email, user.Email));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(jwt, expires);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expires = _clock.GetUtcNow().AddDays(_options.RefreshTokenDays);
        return new RefreshTokenResult(raw, HashRefreshToken(raw), expires);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
