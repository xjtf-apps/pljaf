namespace voks.server.model;

public interface IConvNameChangedObserver : IGrainObserver
{
    Task OnNameChanged(string name);
}
