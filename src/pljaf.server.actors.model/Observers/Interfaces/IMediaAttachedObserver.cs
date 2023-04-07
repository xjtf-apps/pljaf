namespace pljaf.server.model;

public interface IMediaAttachedObserver : IGrainObserver
{
    Task DownloadAttachedMedia(Media? mediaRef);
}
