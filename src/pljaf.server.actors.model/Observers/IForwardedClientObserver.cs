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
{ }
