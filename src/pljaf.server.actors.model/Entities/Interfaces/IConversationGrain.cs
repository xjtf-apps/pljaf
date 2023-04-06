using Orleans;

namespace pljaf.server.model;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<Guid> GetIdAsync();

    Task PostMessageAsync(IMessageGrain message);
    Task<string> GetNameAsync();
    Task<string> GetTopicAsync();
    Task SetNameAsync(string name);
    Task SetTopicAsync(string topic);
    Task<int> GetMessageCountAsync();
    Task<bool> CheckIsGroupConversationAsync();

    Task<List<IUserGrain>> GetMembersAsync();
    Task<List<IUserGrain>> GetInvitedMembersAsync();
    Task LeaveConversationAsync(IUserGrain leavingUser);
    Task InviteToConversationAsync(Invitation invitation);
    Task ResolveInvitationAsync(Invitation invitation, bool accepted);

    Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage);
    Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage);

    Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null);
}