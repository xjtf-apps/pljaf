﻿using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;
using Microsoft.Extensions.Logging;

namespace pljaf.server.model;

public class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<StringValue> _name;
    private readonly IPersistentState<StringValue> _topic;
    private readonly IPersistentState<List<StringValue>> _memberIds;
    private readonly IPersistentState<List<Guid>> _communicationIds;
    private readonly IPersistentState<List<Invitation>> _invitations;

    private readonly ObserverManager<IConvNameChangedObserver> _nameChangedManager;
    private readonly ObserverManager<IConvTopicChangedObserver> _topicChangedManager;
    private readonly ObserverManager<IConvMembersChangedObserver> _membersChangedManager;
    private readonly ObserverManager<IConvInvitesChangedObserver> _invitesChangedManager;
    private readonly ObserverManager<IConvCommunicationChangedObserver> _communicationChangedManager;

    public ConversationGrain(
        ILogger<ConversationGrain> logger,
        [PersistentState(Constants.StoreKeys.Conversation.Name)]IPersistentState<StringValue> name,
        [PersistentState(Constants.StoreKeys.Conversation.Topic)]IPersistentState<StringValue> topic,
        [PersistentState(Constants.StoreKeys.Conversation.Members)]IPersistentState<List<StringValue>> members,
        [PersistentState(Constants.StoreKeys.Conversation.Invitations)]IPersistentState<List<Invitation>> invitations,
        [PersistentState(Constants.StoreKeys.Conversation.Communications)]IPersistentState<List<Guid>> communications)
    {
        _name = name;
        _topic = topic;
        _memberIds = members;
        _invitations = invitations;
        _communicationIds = communications;

        var observersTimespan = TimeSpan.FromMinutes(5);
        _nameChangedManager = new ObserverManager<IConvNameChangedObserver>(observersTimespan, logger);
        _topicChangedManager = new ObserverManager<IConvTopicChangedObserver>(observersTimespan, logger);
        _membersChangedManager = new ObserverManager<IConvMembersChangedObserver>(observersTimespan, logger);
        _invitesChangedManager = new ObserverManager<IConvInvitesChangedObserver>(observersTimespan, logger);
        _communicationChangedManager = new ObserverManager<IConvCommunicationChangedObserver>(observersTimespan, logger);
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<string> GetNameAsync() => await Task.FromResult(_name.State.Value ?? "");
    public async Task<string> GetTopicAsync() => await Task.FromResult(_topic.State.Value ?? "");
    public async Task<int> GetMessageCountAsync() => await Task.FromResult(_communicationIds.State.Count);
    public async Task<bool> CheckIsGroupConversationAsync() => await Task.FromResult(_memberIds.State.Count() > 2);
    public async Task<List<IUserGrain>> GetMembersAsync() => await Task.FromResult(_memberIds.State.Select(userId => GrainFactory.GetGrain<IUserGrain>(userId.Value!)).ToList());
    public async Task<List<IUserGrain>> GetInvitedMembersAsync() => await Task.FromResult(_invitations.State.Select(inv => inv.InvitedId).Distinct().Select(invId => GrainFactory.GetGrain<IUserGrain>(invId)).ToList());

    public async Task PostMessageAsync(IMessageGrain message)
    {
        await _communicationIds.AddItemAndPersistAsync(await message.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(message));
    }

    public async Task SetNameAsync(string name)
    {
        await _name.SetValueAndPersistAsync(new StringValue() { Value = name });
        await _nameChangedManager.Notify(sub => sub.OnNameChanged(name));
    }

    public async Task SetTopicAsync(string topic)
    {
        await _topic.SetValueAndPersistAsync(new StringValue() { Value = topic });
        await _topicChangedManager.Notify(sub => sub.OnTopicChanged(topic));
    }

    public async Task LeaveConversationAsync(IUserGrain leavingUser)
    {
        await _memberIds.RemoveItemAndPersistAsync(new StringValue() { Value = await leavingUser.GetIdAsync() });
        await _membersChangedManager.Notify(sub => sub.OnMemberLeft(leavingUser));
    }

    public async Task InviteToConversationAsync(Invitation invitation)
    {
        await _invitations.AddItemAndPersistAsync(invitation);
        await _invitesChangedManager.Notify(sub => sub.OnMemberInvited(GrainFactory.GetGrain<IUserGrain>(invitation.InviterId), GrainFactory.GetGrain<IUserGrain>(invitation.InvitedId)));
    }

    public async Task<Invitation?> GetInvitationAsync(IUserGrain invitedUser)
    {
        var userId = await invitedUser.GetIdAsync();
        var invitation = await _invitations.State.ToAsyncEnumerable()
            .Where(inv => inv.InvitedId == userId)
            .LastOrDefaultAsync();

        return invitation;
    }

    public async Task ResolveInvitationAsync(Invitation invitation, bool accepted)
    {
        await _invitations.RemoveItemAndPersistAsync(invitation);
        await RemoveOtherInvitationsAsync(GrainFactory.GetGrain<IUserGrain>(invitation.InvitedId));
        if (accepted)
        {
            await _memberIds.AddItemAndPersistAsync(new StringValue() { Value = invitation.InvitedId });
            await _membersChangedManager.Notify(sub => sub.OnMemberJoined(GrainFactory.GetGrain<IUserGrain>(invitation.InvitedId)));
        }
    }

    public async Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage)
    {
        _memberIds.State.Add(new StringValue() { Value = await initiator.GetIdAsync() });
        _memberIds.State.Add(new StringValue() { Value = await contact.GetIdAsync() });
        await _memberIds.WriteStateAsync();
        await _communicationIds.AddItemAndPersistAsync(await firstMessage.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
    }

    public async Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage)
    {
        _memberIds.State.Add(new StringValue() { Value = await initiator.GetIdAsync() });
        foreach (var contact in contacts) _memberIds.State.Add(new StringValue() { Value = await contact.GetIdAsync() });
        await _memberIds.WriteStateAsync(); await _communicationIds.AddItemAndPersistAsync(await firstMessage.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
    }

    public async Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null)
    {
        var query =
            _communicationIds.State.ToAsyncEnumerable()
            .Select(messageId => GrainFactory.GetGrain<IMessageGrain>(messageId))
            .WhereAwait(async message => datetimeFrom == null || (await message.GetTimestampAsync() > datetimeFrom))
            .WhereAwait(async message => datetimeTo == null || (await message.GetTimestampAsync() <= datetimeTo))
            .ToListAsync();

        return await query;
    }

    private async Task RemoveOtherInvitationsAsync(IUserGrain invited)
    {
        var userId = await invited.GetIdAsync();
        await _invitations.State.ToAsyncEnumerable()
            .Where(inv => inv.InvitedId == userId)
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