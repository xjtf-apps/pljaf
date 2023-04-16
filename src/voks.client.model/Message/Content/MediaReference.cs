namespace voks.client.model;

public abstract class MediaReference
{
    public Guid StoreId { get; init; }

    protected abstract Task<byte[]> GetDataAsync();
    public abstract ValueTask<IMediaPreview> GetPreviewAsync();
    public abstract ValueTask<IMediaStoreResult> StoreLocallyAsync();
}
