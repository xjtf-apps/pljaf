namespace voks.server.api;

public class MediaSettingsService
{
    private readonly MediaSettings _media;
    public int MaxAudioSize => _media.MessageAudioMaxSizeBytes;
    public int MaxVideoSize => _media.MessageVideoMaxSizeBytes;
    public int MaxImageSize => _media.MessagePictureMaxSizeBytes;
    public int MaxProfilePictureSize => _media.ProfilePictureMaxSizeBytes;

    public MediaSettingsService(IConfiguration config)
    {
        var configSection = config.GetSection("Media")!;
        var mediaSection = configSection.Get<MediaSettings>()!;
        _media = mediaSection;
    }
}
