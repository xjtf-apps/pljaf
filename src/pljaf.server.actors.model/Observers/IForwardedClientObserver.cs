using pljaf.server.model;

namespace pljaf.server.actors.model;

public interface IForwardedClientObserver :
    IConvNameChangedObserver,
    IConvTopicChangedObserver,
    IConvMembersChangedObserver,
    IConvInvitesChangedObserver,
    IConvCommunicationChangedObserver,
    IMessageMediaAttachedObserver,
    IMessageAuthoredObserver
{
    Task SubscribeToGrain(IGrainFactory grainFactory, IGrain grain)
    {
        if (grain is IMessageGrain message) return SubscribeToMessageGrain(grainFactory, message);
        if (grain is IConversationGrain conversation) return SubscribeToConversationGrain(grainFactory, conversation);
        throw new NotSupportedException(grain.GetType().Name);
    }

    Task UnsubscribeFromGrain(IGrainFactory grainFactory, IGrain grain)
    {
        if (grain is IMessageGrain message) return UnsubscribeFromMessageGrain(grainFactory, message);
        if (grain is IConversationGrain conversation) return UnsubscribeFromConversationGrain(grainFactory, conversation);
        throw new NotSupportedException(grain.GetType().Name);
    }

    Task SubscribeToMessageGrain(IGrainFactory grainFactory, IMessageGrain message)
    {
        var _thisMediaAttachedObserver = grainFactory.CreateObjectReference<IMessageMediaAttachedObserver>(this);
        var _thisMessageAuthoredObserver = grainFactory.CreateObjectReference<IMessageAuthoredObserver>(this);
        var t1 = message.Subscribe(_thisMediaAttachedObserver);
        var t2 = message.Subscribe(_thisMessageAuthoredObserver);
        return Task.WhenAll(t1, t2);
    }

    Task UnsubscribeFromMessageGrain(IGrainFactory grainFactory, IMessageGrain message)
    {
        var _thisMediaAttachedObserver = grainFactory.CreateObjectReference<IMessageMediaAttachedObserver>(this);
        var _thisMessageAuthoredObserver = grainFactory.CreateObjectReference<IMessageAuthoredObserver>(this);
        var t1 = message.Unsubscribe(_thisMediaAttachedObserver);
        var t2 = message.Unsubscribe(_thisMessageAuthoredObserver);
        return Task.WhenAll(t1, t2);
    }

    Task SubscribeToConversationGrain(IGrainFactory grainFactory, IConversationGrain conversation)
    {
        var _thisNameObserver = grainFactory.CreateObjectReference<IConvNameChangedObserver>(this);
        var _thisTopicObserver = grainFactory.CreateObjectReference<IConvTopicChangedObserver>(this);
        var _thisMembersObserver = grainFactory.CreateObjectReference<IConvMembersChangedObserver>(this);
        var _thisInvitesObserver = grainFactory.CreateObjectReference<IConvInvitesChangedObserver>(this);
        var _thisCommunicationObserver = grainFactory.CreateObjectReference<IConvCommunicationChangedObserver>(this);
        var t1 = conversation.Subscribe(_thisNameObserver);
        var t2 = conversation.Subscribe(_thisTopicObserver);
        var t3 = conversation.Subscribe(_thisMembersObserver);
        var t4 = conversation.Subscribe(_thisInvitesObserver);
        var t5 = conversation.Subscribe(_thisCommunicationObserver);
        return Task.WhenAll(t1, t2, t3, t4, t5);
    }

    Task UnsubscribeFromConversationGrain(IGrainFactory grainFactory, IConversationGrain conversation)
    {
        var _thisNameObserver = grainFactory.CreateObjectReference<IConvNameChangedObserver>(this);
        var _thisTopicObserver = grainFactory.CreateObjectReference<IConvTopicChangedObserver>(this);
        var _thisMembersObserver = grainFactory.CreateObjectReference<IConvMembersChangedObserver>(this);
        var _thisInvitesObserver = grainFactory.CreateObjectReference<IConvInvitesChangedObserver>(this);
        var _thisCommunicationObserver = grainFactory.CreateObjectReference<IConvCommunicationChangedObserver>(this);
        var t1 = conversation.Unsubscribe(_thisNameObserver);
        var t2 = conversation.Unsubscribe(_thisTopicObserver);
        var t3 = conversation.Unsubscribe(_thisMembersObserver);
        var t4 = conversation.Unsubscribe(_thisInvitesObserver);
        var t5 = conversation.Unsubscribe(_thisCommunicationObserver);
        return Task.WhenAll(t1, t2, t3, t4, t5);
    }
}

