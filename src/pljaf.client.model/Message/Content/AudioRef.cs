﻿namespace pljaf.client.model;

public class AudioRef : MediaReference
{
    public override ValueTask<IMediaPreview> GetPreviewAsync()
    {
        throw new NotImplementedException();
    }

    public override ValueTask<IMediaStoreResult> StoreLocallyAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task<byte[]> GetDataAsync()
    {
        throw new NotImplementedException();
    }
}
