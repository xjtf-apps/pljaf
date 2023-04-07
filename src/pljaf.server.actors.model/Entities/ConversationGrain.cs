using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;
using Microsoft.Extensions.Logging;

namespace pljaf.server.model;

public class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<string?> _name;
    private readonly IPersistentState<string?> _topic;
    private readonly IPersistentState<List<IUserGrain>> _members;
    private readonly IPersistentState<List<Invitation>> _invitations;
    private readonly IPersistentState<List<IMessageGrain>> _communications;

    private readonly ObserverManager<IConvNameChangedObserver> _nameChangedManager;
    private readonly ObserverManager<IConvTopicChangedObserver> _topicChangedManager;
    private readonly ObserverManager<IConvMembersChangedObserver> _membersChangedManager;
    private readonly ObserverManager<IConvInvitesChangedObserver> _invitesChangedManager;
    private readonly ObserverManager<IConvCommunicationChangedObserver> _communicationChangedManager;

    public ConversationGrain(
        ILogger<ConversationGrain> logger,
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

        var observersTimespan = TimeSpan.FromMinutes(5);
        _nameChangedManager = new ObserverManager<IConvNameChangedObserver>(observersTimespan, logger);
        _topicChangedManager = new ObserverManager<IConvTopicChangedObserver>(observersTimespan, logger);
        _membersChangedManager = new ObserverManager<IConvMembersChangedObserver>(observersTimespan, logger);
        _invitesChangedManager = new ObserverManager<IConvInvitesChangedObserver>(observersTimespan, logger);
        _communicationChangedManager = new ObserverManager<IConvCommunicationChangedObserver>(observersTimespan, logger);
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<string> GetNameAsync() => await Task.FromResult(_name.State ?? "");
    public async Task<string> GetTopicAsync() => await Task.FromResult(_topic.State ?? "");
    public async Task<List<IUserGrain>> GetMembersAsync() => await Task.FromResult(_members.State);
    public async Task<bool> CheckIsGroupConversationAsync() => await Task.FromResult(_members.State.Count() > 2);
    public async Task<List<IUserGrain>> GetInvitedMembersAsync() => await Task.FromResult(_invitations.State.Select(inv => inv.Invited).Distinct().ToList());
    public async Task<int> GetMessageCountAsync() => await Task.FromResult(_communications.State.Count);

    public async Task PostMessageAsync(IMessageGrain message)
    {
        await _communications.AddItemAndPersistAsync(message);
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(message));
    }

    public async Task SetNameAsync(string name)
    {
        await _name.SetValueAndPersistAsync(name);
        await _nameChangedManager.Notify(sub => sub.OnNameChanged(name));
    }

    public async Task SetTopicAsync(string topic)
    {
        await _topic.SetValueAndPersistAsync(topic);
        await _topicChangedManager.Notify(sub => sub.OnTopicChanged(topic));
    }

    public async Task LeaveConversationAsync(IUserGrain leavingUser)
    {
        await _members.RemoveItemAndPersistAsync(leavingUser);
        await _membersChangedManager.Notify(sub => sub.OnMemberLeft(leavingUser));
    }

    public async Task InviteToConversationAsync(Invitation invitation)
    {
        await _invitations.AddItemAndPersistAsync(invitation);
        await _invitesChangedManager.Notify(sub => sub.OnMemberInvited(invitation.Inviter, invitation.Invited));
    }

    public async Task<Invitation?> GetInvitationAsync(IUserGrain invitedUser)
    {
        var userId = await invitedUser.GetIdAsync();
        var invitation = await _invitations.State.ToAsyncEnumerable()
            .WhereAwait(async inv => (await inv.Invited.GetIdAsync()) == userId)
            .LastOrDefaultAsync();

        return invitation;
    }

    public async Task ResolveInvitationAsync(Invitation invitation, bool accepted)
    {
        await _invitations.RemoveItemAndPersistAsync(invitation);
        await RemoveOtherInvitationsAsync(invitation.Invited);
        if (accepted)
        {
            await _members.AddItemAndPersistAsync(invitation.Invited);
            await _membersChangedManager.Notify(sub => sub.OnMemberJoined(invitation.Invited));
        }
    }

    public async Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage)
    {
        _members.State.Add(initiator);
        _members.State.Add(contact);
        await _members.WriteStateAsync();
        await _communications.AddItemAndPersistAsync(firstMessage);
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
    }

    public async Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage)
    {
        _members.State.Add(initiator);
        foreach (var contact in contacts) _members.State.Add(contact);
        await _members.WriteStateAsync(); await _communications.AddItemAndPersistAsync(firstMessage);
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
    }

    public async Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null)
    {
        var query =
            _communications.State.ToAsyncEnumerable()
            .WhereAwait(async message => datetimeFrom == null || (await message.GetTimestampAsync() > datetimeFrom))
            .WhereAwait(async message => datetimeTo == null || (await message.GetTimestampAsync() <= datetimeTo))
            .ToListAsync();

        return await query;
    }

    private async Task RemoveOtherInvitationsAsync(IUserGrain invited)
    {
        var userId = await invited.GetIdAsync();
        await _invitations.State.ToAsyncEnumerable()
            .WhereAwait(async inv => (await inv.Invited.GetIdAsync()) == userId)
            .ForEachAsync(async inv => await _invitations.RemoveItemAndPersistAsync(inv));
    }

    #region observers
    public Task Subscribe(IConvNameChangedObserver nameChangedObserver)
    {
        _nameChangedManager.Subscribe(nameChangedObserver, nameChangedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IConvTopicChangedObserver topicChangedObserver)
    {
        _topicChangedManager.Subscribe(topicChangedObserver, topicChangedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IConvMembersChangedObserver membersChangedObserver)
    {
        _membersChangedManager.Subscribe(membersChangedObserver, membersChangedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IConvInvitesChangedObserver invitesChangedObserver)
    {
        _invitesChangedManager.Subscribe(invitesChangedObserver, invitesChangedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IConvCommunicationChangedObserver communicationChangedObserver)
    {
        _communicationChangedManager.Subscribe(communicationChangedObserver, communicationChangedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IConvNameChangedObserver nameChangedObserver)
    {
        _nameChangedManager.Unsubscribe(nameChangedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IConvTopicChangedObserver topicChangedObserver)
    {
        _topicChangedManager.Unsubscribe(topicChangedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IConvMembersChangedObserver membersChangedObserver)
    {
        _membersChangedManager.Unsubscribe(membersChangedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IConvInvitesChangedObserver invitesChangedObserver)
    {
        _invitesChangedManager.Unsubscribe(invitesChangedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IConvCommunicationChangedObserver communicationChangedObserver)
    {
        _communicationChangedManager.Unsubscribe(communicationChangedObserver);
        return Task.CompletedTask;
    }
    #endregion
}