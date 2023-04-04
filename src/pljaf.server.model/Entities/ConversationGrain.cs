using Orleans;
using Orleans.Runtime;

namespace pljaf.server.model;

public class ConversationGrain : Grain, IConversationGrain
{
    public Guid Id => this.GetGrainId().GetGuidKey();

    private readonly IPersistentState<string?> _name;
    private readonly IPersistentState<string?> _topic;
    private readonly IPersistentState<List<IUserGrain>> _members;
    private readonly IPersistentState<List<Invitation>> _invitations;
    private readonly IPersistentState<List<IMessageGrain>> _communications;

    public ConversationGrain(
        [PersistentState(Constants.StoreKeys.Conversation.Name)]IPersistentState<string?> name,
        [PersistentState(Constants.StoreKeys.Conversation.Topic)]IPersistentState<string?> topic,
        [PersistentState(Constants.StoreKeys.Conversation.Members)]IPersistentState<List<IUserGrain>> members,
        [PersistentState(Constants.StoreKeys.Conversation.Invitations)]IPersistentState<List<Invitation>> invitations,
        [PersistentState(Constants.StoreKeys.Conversation.Communications)]IPersistentState<List<IMessageGrain>> communications)
    {
        _name = name;
        _topic = topic;
        _members = members;
        _invitations = invitations;
        _communications = communications;
    }

    public async Task<string> GetNameAsync() => await Task.FromResult(_name.State ?? "");
    public async Task<string> GetTopicAsync() => await Task.FromResult(_topic.State ?? "");

    public async Task PostMessageAsync(IMessageGrain message) => await _communications.AddItemAndPersistAsync(message);
    public async Task SetNameAsync(string name) => await _name.SetValueAndPersistAsync(name);
    public async Task SetTopicAsync(string topic) => await _topic.SetValueAndPersistAsync(topic);

    public async Task<List<IUserGrain>> GetMembersAsync() => await Task.FromResult(_members.State);
    public async Task LeaveConversationAsync(IUserGrain leavingUser) => await _members.RemoveItemAndPersistAsync(leavingUser);
    public async Task InviteToConversationAsync(Invitation invitation) => await _invitations.AddItemAndPersistAsync(invitation);
    public async Task ResolveInvitationAsync(Invitation invitation, bool accepted)
    {
        await _invitations.RemoveItemAndPersistAsync(invitation);
        if (accepted) await _members.AddItemAndPersistAsync(invitation.Invited);
    }

    public async Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage)
    {
        _members.State.Add(initiator);
        _members.State.Add(contact);
        await _members.WriteStateAsync();
        await _communications.AddItemAndPersistAsync(firstMessage);
    }

    public async Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage)
    {
        _members.State.Add(initiator);
        foreach (var contact in contacts) _members.State.Add(contact);
        await _members.WriteStateAsync(); await _communications.AddItemAndPersistAsync(firstMessage);
    }

    public async Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null)
    {
        var query =
            _communications.State
            .Where(message => datetimeFrom == null || (message.Timestamp > datetimeFrom))
            .Where(message => datetimeTo == null || (message.Timestamp <= datetimeTo))
            .ToList();

        return await Task.FromResult(query);
    }

    public async Task<int> GetMessageCountAsync() => await Task.FromResult(_communications.State.Count);
}
