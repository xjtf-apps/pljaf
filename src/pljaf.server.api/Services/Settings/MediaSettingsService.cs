namespace pljaf.server.api;

public class MediaSettingsService
{
    private readonly MediaSettings _media;
    public int MaxProfilePictureSize => _media.ProfilePictureMaxSizeBytes;

    public MediaSettingsService(IConfiguration config)
    {
        var configSection = config.GetSection("Media")!;
        var mediaSection = configSection.Get<MediaSettings>()!;
        _media = mediaSection;
    }
}
