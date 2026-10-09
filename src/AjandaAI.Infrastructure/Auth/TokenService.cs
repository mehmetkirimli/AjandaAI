// ITokenService implementasyonu: HMAC-SHA256 imzalı JWT access token ve kriptografik rastgele opak token'lar.
// Claim'ler: "sub" = kullanıcı Id, "role" = UserRole adı, "jti"; issuer/audience/süre JwtOptions'tan gelir.
// Opak token'lar base64url (URL'de güvenli) metindir; DB'ye UTF-8 baytlarının SHA-256 hash'i (hex) yazılır.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AjandaAI.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private const int RefreshTokenBytes = 64;
    private const int VerificationTokenBytes = 32;
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(12);

    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(User user, DateTimeOffset now)
    {
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtOptions.UserIdClaim, user.Id.ToString()),
            new Claim(JwtOptions.RoleClaim, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        var jwt = new JwtSecurityToken(
            _options.Issuer, _options.Audience, claims,
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime, signingCredentials: _credentials);
        return new AccessTokenResult(_handler.WriteToken(jwt), expires);
    }

    public RandomTokenResult CreateRefreshToken(DateTimeOffset now) =>
        CreateRandomToken(RefreshTokenBytes, now.AddDays(_options.RefreshTokenDays));

    public RandomTokenResult CreateVerificationToken(DateTimeOffset now) =>
        CreateRandomToken(VerificationTokenBytes, now.Add(VerificationLifetime));

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private RandomTokenResult CreateRandomToken(int byteCount, DateTimeOffset expiresAt)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteCount))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new RandomTokenResult(token, HashToken(token), expiresAt);
    }
}
