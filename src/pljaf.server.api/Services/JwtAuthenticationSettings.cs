namespace pljaf.server.api;

public class JwtAuthenticationSettings
{
    private readonly JwtSettings _jwtSettings;
    public string Secret => _jwtSettings.Secret;
    public string ValidIssuer => _jwtSettings.ValidIssuer;
    public string ValidAudience => _jwtSettings.ValidAudience;
    public int TokenValidityInMinutes => _jwtSettings.TokenValidityInMinutes;
    public int RefreshTokenValidityInDays => _jwtSettings.RefreshTokenValidityInDays;

    public JwtAuthenticationSettings(IConfiguration config)
    {
        var configSection = config.GetSection("Jwt")!;
        var jwtSection = configSection.Get<JwtSettings>()!;
        _jwtSettings = jwtSection;
    }
}
