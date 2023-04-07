namespace pljaf.server.model;

public interface IMessageMediaAttachedObserver : IGrainObserver
{
    Task DownloadAttachedMedia(Media? mediaRef);
}
