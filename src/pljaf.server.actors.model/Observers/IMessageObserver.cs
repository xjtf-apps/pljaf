namespace pljaf.server.model;

public interface IMessageObserver :
    IMessageAuthoredObserver,
    IMessageMediaAttachedObserver
{
    event EventHandler<string>? OnChange;

    Task Subscribe(IGrainFactory grainFactory);
    Task Unsubscribe(IGrainFactory grainFactory);

    Task SubscribeToMessageGrain(IGrainFactory grainFactory, IMessageGrain message)
    {
        var _thisMessageAuthoredObserver = grainFactory.CreateObjectReference<IMessageAuthoredObserver>(this);
        var _thisMediaAttachedObserver = grainFactory.CreateObjectReference<IMessageMediaAttachedObserver>(this);
        var t1 = message.Subscribe(_thisMessageAuthoredObserver);
        var t2 = message.Subscribe(_thisMediaAttachedObserver);
        return Task.WhenAll(t1, t2);
    }

    Task UnsubscribeFromMessageGrain(IGrainFactory grainFactory, IMessageGrain message)
    {
        var _thisMessageAuthoredObserver = grainFactory.CreateObjectReference<IMessageAuthoredObserver>(this);
        var _thisMediaAttachedObserver = grainFactory.CreateObjectReference<IMessageMediaAttachedObserver>(this);
        var t1 = message.Unsubscribe(_thisMessageAuthoredObserver);
        var t2 = message.Unsubscribe(_thisMediaAttachedObserver);
        return Task.WhenAll(t1, t2);
    }
}

