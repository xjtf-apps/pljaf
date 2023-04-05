using Orleans;
using Orleans.Runtime;

namespace pljaf.server.model;

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<Tokens> _tokens;
    private readonly IPersistentState<Options> _options;
    private readonly IPersistentState<Profile> _profile;
    private readonly IPersistentState<List<IUserGrain>> _contacts;
    private readonly IPersistentState<List<IConversationGrain>> _conversations;

    public UserGrain(
        [PersistentState(Constants.StoreKeys.User.Tokens)] IPersistentState<Tokens> tokens,
        [PersistentState(Constants.StoreKeys.User.Options)] IPersistentState<Options> options,
        [PersistentState(Constants.StoreKeys.User.Profile)] IPersistentState<Profile> profile,
        [PersistentState(Constants.StoreKeys.User.Contacts)] IPersistentState<List<IUserGrain>> contacts,
        [PersistentState(Constants.StoreKeys.User.Conversations)] IPersistentState<List<IConversationGrain>> conversations)
    {
        _tokens = tokens;
        _options = options;
        _profile = profile;
        _contacts = contacts;
        _conversations = conversations;
    }

    public async Task<string> GetIdAsync() => await Task.FromResult(this.GetPrimaryKeyString());
    public async Task<Media?> GetAvatarAsync() => await Task.FromResult(_profile.State.ProfilePicture);

    public async Task<Tokens> GetTokensAsync() => await Task.FromResult(_tokens.State);
    public async Task<Options> GetOptionsAsync() => await Task.FromResult(_options.State);
    public async Task<Profile> GetProfileAsync() => await Task.FromResult(_profile.State);
    public async Task<List<IUserGrain>> GetContactsAsync() => await Task.FromResult(_contacts.State);
    public async Task<List<IConversationGrain>> GetConversationsAsync() => await Task.FromResult(_conversations.State);


    public async Task SetTokensAsync(Tokens tokens) => await _tokens.SetValueAndPersistAsync(tokens);
    public async Task SetOptionsAsync(Options options) => await _options.SetValueAndPersistAsync(options);
    public async Task SetProfileAsync(Profile profile) => await _profile.SetValueAndPersistAsync(profile);
    public async Task AddContactAsync(IUserGrain contact) => await _contacts.AddItemAndPersistAsync(contact);
    public async Task RemoveContactAsync(IUserGrain contact) => await _contacts.RemoveItemAndPersistAsync(contact);
}
