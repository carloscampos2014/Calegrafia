using System.IdentityModel.Tokens.Jwt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Calegrafia.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Calegrafia.Infrastructure.Services;

public sealed class JwtService : IJwtService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenExpiry = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _refreshTokenExpiry = TimeSpan.FromDays(30);

    public JwtService(string privateKeyPem, string publicKeyPem, string issuer, string audience)
    {
        _issuer = issuer;
        _audience = audience;

        var rsaPrivate = RSA.Create();
        rsaPrivate.ImportFromPem(privateKeyPem);
        _privateKey = new RsaSecurityKey(rsaPrivate);

        var rsaPublic = RSA.Create();
        rsaPublic.ImportFromPem(publicKeyPem);
        _publicKey = new RsaSecurityKey(rsaPublic);
    }

    public string GerarAccessToken(Guid contaId, string email, Guid? perfilId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, contaId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (perfilId.HasValue)
            claims.Add(new Claim("perfil_id", perfilId.Value.ToString()));

        var credentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(_accessTokenExpiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, DateTime ExpiraEm) GerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var expiraEm = DateTime.UtcNow.Add(_refreshTokenExpiry);
        return (token, expiraEm);
    }

    public TokenPayload? ValidarAccessToken(string token)
    {
        // Desabilitar mapeamento automático de claims para preservar nomes originais (sub, email, etc.)
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _publicKey,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out var validatedToken);
            var jwt = (JwtSecurityToken)validatedToken;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var perfilIdStr = principal.FindFirst("perfil_id")?.Value;

            if (sub is null || email is null)
                return null;

            Guid? perfilId = perfilIdStr is not null ? Guid.Parse(perfilIdStr) : null;

            return new TokenPayload(
                ContaId: Guid.Parse(sub),
                Email: email,
                PerfilId: perfilId,
                ExpiraEm: jwt.ValidTo);
        }
        catch
        {
            return null;
        }
    }
}
