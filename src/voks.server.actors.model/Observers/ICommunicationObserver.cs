namespace voks.server.model;

public interface ICommunicationObserver :
    IConvNameChangedObserver,
    IConvTopicChangedObserver,
    IConvMembersChangedObserver,
    IConvInvitesChangedObserver,
    IConvCommunicationChangedObserver
{
    Task SubscribeToGrain();
    Task UnsubscribeFromGrain();

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