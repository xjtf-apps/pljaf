using Orleans;

namespace pljaf.server.model;

public interface IUserGrain : IGrainWithStringKey
{
    Task<string> GetIdAsync();
    Task<Media?> GetAvatarAsync();

    Task<Options> GetOptionsAsync();
    Task SetOptionsAsync(Options options);

    Task<Profile> GetProfileAsync();
    Task SetProfileAsync(Profile profile);

    Task<List<IUserGrain>> GetContactsAsync();
    Task AddContactAsync(IUserGrain contact);
    Task RemoveContactAsync(IUserGrain contact);

    Task<List<IConversationGrain>> GetConversationsAsync();
}