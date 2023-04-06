namespace pljaf.server.api;

public class TwillioUserVerificationSettings
{
    public string AuthToken => _config.AuthToken;
    public string AccountSid => _config.AccountSid;
    public string ServiceSid => _config.ServiceSid;

    private readonly VerificationSettings.TwillioSettings _config;

    public TwillioUserVerificationSettings(IConfiguration config)
    {
        var configSection = config.GetRequiredSection("Verification");
        var verificationConfig = configSection.Get<VerificationSettings>()!;
        var twillioConfig = verificationConfig.Twillio;

        _config = twillioConfig;
    }
}
