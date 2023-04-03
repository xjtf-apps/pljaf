namespace pljaf.client.model;

public interface IMediaSource
{
    Task<byte[]> GetMediaDataAsync();
}