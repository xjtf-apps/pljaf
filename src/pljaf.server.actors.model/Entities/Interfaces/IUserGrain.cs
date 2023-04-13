using Orleans;
using Orleans.Concurrency;

namespace pljaf.server.model;

public interface IUserGrain : IGrainWithStringKey
{
    [AlwaysInterleave] Task<string> GetIdAsync();
    [AlwaysInterleave] Task<Media?> GetAvatarAsync();
    [AlwaysInterleave] Task<Options> GetOptionsAsync();
    [AlwaysInterleave] Task<Profile> GetProfileAsync();
    [AlwaysInterleave] Task<List<IUserGrain>> GetContactsAsync();
    [AlwaysInterleave] Task<Tokens> GetTokensAsync();
    [AlwaysInterleave] Task<int> GetConversationsCountAsync();
    [AlwaysInterleave] Task<List<IConversationGrain>> GetConversationsAsync();

    Task SetOptionsAsync(Options options);
    Task SetProfileAsync(Profile profile);
    Task AddContactAsync(IUserGrain contact);
    Task RemoveContactAsync(IUserGrain contact);
    Task SetTokensAsync(Tokens tokens);

    Task Internal_AddToConversationAsync(Guid conversationId);
    Task Internal_RemoveFromConversationAsync(Guid conversationId);
}