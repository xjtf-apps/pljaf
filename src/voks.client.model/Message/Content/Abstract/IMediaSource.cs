namespace voks.client.model;

public interface IMediaSource
{
    Task<byte[]> GetMediaDataAsync();
}