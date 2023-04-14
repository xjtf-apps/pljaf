using Orleans;
using Orleans.Concurrency;

namespace pljaf.server.model;

public interface IUserGrain : IGrainWithStringKey
{
    [AlwaysInterleave] Task<string> GetIdAsync();
    [AlwaysInterleave] Task<Media?> GetAvatarAsync();
    [AlwaysInterleave] Task<Options> GetOptionsAsync();
    [AlwaysInterleave] Task<Profile> GetProfileAsync();
    [AlwaysInterleave] Task<Tokens> GetTokensAsync();
    [AlwaysInterleave] Task<int> GetConversationsCountAsync();
    [AlwaysInterleave] Task<List<IUserGrain>> GetContactsAsync();
    [AlwaysInterleave] Task<List<IConversationGrain>> GetConversationsAsync();
    [AlwaysInterleave] Task<List<IConversationInviteGrain>> GetInvitationsAsync();

    Task SetTokensAsync(Tokens tokens);
    Task SetOptionsAsync(Options options);
    Task SetProfileAsync(Profile profile);
    Task AddContactAsync(IUserGrain contact);
    Task RemoveContactAsync(IUserGrain contact);
    Task InviteAsync(IConversationInviteGrain invite);
    Task ResolveInvitationAsync(IConversationInviteGrain invite, bool solution);

    Task Internal_AddToConversationAsync(Guid conversationId);
    Task Internal_RemoveFromConversationAsync(Guid conversationId);
}