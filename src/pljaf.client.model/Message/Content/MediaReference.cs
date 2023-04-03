namespace pljaf.client.model;

public abstract class MediaReference
{
    protected abstract Task<byte[]> GetDataAsync();
    public abstract ValueTask<IMediaPreview> GetPreviewAsync();
    public abstract ValueTask<IMediaStoreResult> StoreLocallyAsync();
}
