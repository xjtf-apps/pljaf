using Orleans;
using Orleans.Concurrency;

namespace voks.server.model;

public interface IConversationGrain : IGrainWithGuidKey
{
    [AlwaysInterleave] Task<Guid> GetIdAsync();
    [AlwaysInterleave] Task<string> GetNameAsync();
    [AlwaysInterleave] Task<string> GetTopicAsync();
    [AlwaysInterleave] Task<int> GetMessageCountAsync();
    [AlwaysInterleave] Task<bool> CheckIsGroupConversationAsync();
    [AlwaysInterleave] Task<List<IUserGrain>> GetMembersAsync();
    [AlwaysInterleave] Task<IMessageGrain> GetLastMessageAsync();
    [AlwaysInterleave] Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null);

    Task SetNameAsync(string name);
    Task SetTopicAsync(string topic);
    Task PostMessageAsync(IMessageGrain message);
    Task LeaveConversationAsync(IUserGrain leavingUser);
    Task EnterConversationAsync(IUserGrain enteringUser);
    Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage);
    Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage);

    #region observers
    Task Subscribe(IConvNameChangedObserver nameChangedObserver);
    Task Subscribe(IConvTopicChangedObserver topicChangedObserver);
    Task Subscribe(IConvMembersChangedObserver membersChangedObserver);
    Task Subscribe(IConvInvitesChangedObserver invitesChangedObserver);
    Task Subscribe(IConvCommunicationChangedObserver communicationChangedObserver);
    Task Unsubscribe(IConvNameChangedObserver nameChangedObserver);
    Task Unsubscribe(IConvTopicChangedObserver topicChangedObserver);
    Task Unsubscribe(IConvMembersChangedObserver membersChangedObserver);
    Task Unsubscribe(IConvInvitesChangedObserver invitesChangedObserver);
    Task Unsubscribe(IConvCommunicationChangedObserver communicationChangedObserver);
    #endregion
}