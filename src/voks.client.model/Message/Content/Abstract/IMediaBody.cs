namespace voks.client.model;

public interface IMediaBody : IMediaSource
{
    MediaReference GetMediaReference();
}