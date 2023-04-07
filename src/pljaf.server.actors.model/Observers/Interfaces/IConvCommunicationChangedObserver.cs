namespace pljaf.server.model;

public interface IConvCommunicationChangedObserver : IGrainObserver
{
    Task OnMessagePosted(IMessageGrain message);
}
