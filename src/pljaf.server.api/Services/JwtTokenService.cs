using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace pljaf.server.api;

public class JwtTokenService
{
    private readonly JwtAuthenticationSettings _jwtSettings;
    private SymmetricSecurityKey SecurityKey => new(SecretBytes);
    private byte[] SecretBytes => Encoding.UTF8.GetBytes(_jwtSettings.Secret);
    private SigningCredentials SigningCredentials => new(SecurityKey, SecurityAlgorithms.HmacSha256);

    public JwtTokenService(JwtAuthenticationSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }
    
    public JwtSecurityToken CreateToken(Claim[] claims)
    {
        return new JwtSecurityToken(
            issuer: _jwtSettings.ValidIssuer,
            audience: _jwtSettings.ValidAudience,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenValidityInMinutes),
            claims: claims,
            signingCredentials: SigningCredentials
            );
    }

    public string CreateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime CalculateRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenValidityInDays);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var parameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKey,
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, parameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}