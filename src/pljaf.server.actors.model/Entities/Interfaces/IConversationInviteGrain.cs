using Orleans.Concurrency;

namespace pljaf.server.model;

public interface IConversationInviteGrain : IGrainWithGuidKey
{
    [AlwaysInterleave] Task<Guid> GetIdAsync();
    [AlwaysInterleave] Task<IUserGrain> GetInviterAsync();
    [AlwaysInterleave] Task<DateTime> GetInviteTimestampAsync();
    [AlwaysInterleave] Task<IConversationGrain> GetConversationAsync();

    Task InviteUserAsync(IUserGrain inviter, IUserGrain invitedUser, IConversationGrain conversation);
}
