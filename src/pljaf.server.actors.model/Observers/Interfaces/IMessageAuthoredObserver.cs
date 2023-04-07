namespace pljaf.server.model;

public interface IMessageAuthoredObserver : IGrainObserver
{
    Task ReceiveSentConfirmation(DateTime timestamp);
}
