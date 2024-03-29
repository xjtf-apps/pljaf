﻿namespace voks.server.api;

public class JwtSettingsService
{
    private readonly JwtSettings _jwtSettings;
    public string Secret => _jwtSettings.Secret;
    public string ValidIssuer => _jwtSettings.ValidIssuer;
    public string ValidAudience => _jwtSettings.ValidAudience;
    public int TokenValidityInMinutes => _jwtSettings.TokenValidityInMinutes;
    public int RefreshTokenValidityInDays => _jwtSettings.RefreshTokenValidityInDays;

    public JwtSettingsService(IConfiguration config)
    {
        var configSection = config.GetSection("Jwt")!;
        var jwtSection = configSection.Get<JwtSettings>()!;
        _jwtSettings = jwtSection;
    }
}
