namespace voks.server.api;

public class MediaSettings
{
    public required int ProfilePictureMaxSizeBytes { get; set; }
    public required int MessagePictureMaxSizeBytes { get; set; }
    public required int MessageVideoMaxSizeBytes { get; set; }
    public required int MessageAudioMaxSizeBytes { get; set; }
}
