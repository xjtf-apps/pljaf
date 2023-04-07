namespace pljaf.server.model;

public interface IConvTopicChangedObserver : IGrainObserver
{
    Task OnTopicChanged(string topic);
}
