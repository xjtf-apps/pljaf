using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;

namespace pljaf.server.model;

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<Tokens> _tokens;
    private readonly IPersistentState<Options> _options;
    private readonly IPersistentState<Profile> _profile;
    private readonly IPersistentState<List<Guid>> _invitationIds;
    private readonly IPersistentState<List<Guid>> _conversationIds;
    private readonly IPersistentState<List<StringValue>> _contactIds;

    public UserGrain(
        [PersistentState(Constants.StoreKeys.User.Tokens)] IPersistentState<Tokens> tokens,
        [PersistentState(Constants.StoreKeys.User.Options)] IPersistentState<Options> options,
        [PersistentState(Constants.StoreKeys.User.Invitations)] IPersistentState<List<Guid>> invitations,
        [PersistentState(Constants.StoreKeys.User.Contacts)] IPersistentState<List<StringValue>> contacts,
        [PersistentState(Constants.StoreKeys.User.Conversations)] IPersistentState<List<Guid>> conversations,
        [PersistentState(Constants.StoreKeys.User.Profile, Constants.Stores.MediaStore)] IPersistentState<Profile> profile)
    {
        _tokens = tokens;
        _options = options;
        _profile = profile;
        _contactIds = contacts;
        _invitationIds = invitations;
        _conversationIds = conversations;
    }

    public async Task<string> GetIdAsync() => await Task.FromResult(this.GetPrimaryKeyString());
    public async Task<Media?> GetAvatarAsync() => await Task.FromResult(_profile.State.ProfilePicture);

    public async Task<Tokens> GetTokensAsync() => await Task.FromResult(_tokens.State);
    public async Task<Options> GetOptionsAsync() => await Task.FromResult(_options.State);
    public async Task<Profile> GetProfileAsync() => await Task.FromResult(_profile.State);
    public async Task<int> GetConversationsCountAsync() => await Task.FromResult(_conversationIds.State.Distinct().Count());
    public async Task<List<IUserGrain>> GetContactsAsync() => await Task.FromResult(_contactIds.State.Select(contactId => GrainFactory.GetGrain<IUserGrain>(contactId.Value!)).ToList());
    public async Task<List<IConversationInviteGrain>> GetInvitationsAsync() => await Task.FromResult(_invitationIds.State.Select(i => GrainFactory.GetGrain<IConversationInviteGrain>(i)).ToList());
    public async Task<List<IConversationGrain>> GetConversationsAsync() => await Task.FromResult(_conversationIds.State.Distinct().Select(convId => GrainFactory.GetGrain<IConversationGrain>(convId)).ToList());


    public async Task SetTokensAsync(Tokens tokens) => await _tokens.SetValueAndPersistAsync(tokens);
    public async Task SetOptionsAsync(Options options) => await _options.SetValueAndPersistAsync(options);
    public async Task SetProfileAsync(Profile profile) => await _profile.SetValueAndPersistAsync(profile);
    public async Task InviteAsync(IConversationInviteGrain invite) => await _invitationIds.AddItemAndPersistAsync(await invite.GetIdAsync());
    public async Task AddContactAsync(IUserGrain contact) => await _contactIds.AddItemAndPersistAsync(StringValue.New(await contact.GetIdAsync()));
    public async Task RemoveContactAsync(IUserGrain contact) => await _contactIds.RemoveItemAndPersistAsync(StringValue.New(await contact.GetIdAsync()));

    public async Task Internal_AddToConversationAsync(Guid conversationId) => await _conversationIds.AddItemAndPersistAsync(conversationId);
    public async Task Internal_RemoveFromConversationAsync(Guid conversationId) => await _conversationIds.RemoveItemAndPersistAsync(conversationId);

    public async Task ResolveInvitationAsync(IConversationInviteGrain invite, bool solution)
    {
        var inviteId = await invite.GetIdAsync();
        await _invitationIds.RemoveItemAndPersistAsync(inviteId);
        if (solution)
        {
            var conversation = await invite.GetConversationAsync();
            await conversation.EnterConversationAsync(this);
        }
    }
}
