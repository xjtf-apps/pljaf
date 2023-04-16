using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;
using Microsoft.Extensions.Logging;

namespace voks.server.model;

public class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<StringValue> _name;
    private readonly IPersistentState<StringValue> _topic;
    private readonly IPersistentState<List<StringValue>> _memberIds;
    private readonly IPersistentState<List<Guid>> _communicationIds;

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
        [PersistentState(Constants.StoreKeys.Conversation.Communications)]IPersistentState<List<Guid>> communications)
    {
        _name = name;
        _topic = topic;
        _memberIds = members;
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

    public async Task PostMessageAsync(IMessageGrain message)
    {
        await _communicationIds.AddItemAndPersistAsync(await message.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(message));
    }

    public async Task SetNameAsync(string name)
    {
        await _name.SetValueAndPersistAsync(StringValue.New(name));
        await _nameChangedManager.Notify(sub => sub.OnNameChanged(name));
    }

    public async Task SetTopicAsync(string topic)
    {
        await _topic.SetValueAndPersistAsync(StringValue.New(topic));
        await _topicChangedManager.Notify(sub => sub.OnTopicChanged(topic));
    }

    public async Task EnterConversationAsync(IUserGrain enteringUser)
    {
        await _memberIds.AddItemAndPersistAsync(StringValue.New(await enteringUser.GetIdAsync()));
        await _membersChangedManager.Notify(sub => sub.OnMemberJoined(enteringUser));
    }

    public async Task LeaveConversationAsync(IUserGrain leavingUser)
    {
        await _memberIds.RemoveItemAndPersistAsync(StringValue.New(await leavingUser.GetIdAsync()));
        await _membersChangedManager.Notify(sub => sub.OnMemberLeft(leavingUser));
    }

    public async Task InitializeNewConversationAsync(IUserGrain initiator, IUserGrain contact, IMessageGrain firstMessage)
    {
        _memberIds.State.Add(StringValue.New(await initiator.GetIdAsync()));
        _memberIds.State.Add(StringValue.New(await contact.GetIdAsync()));
        await _memberIds.WriteStateAsync();
        await _communicationIds.AddItemAndPersistAsync(await firstMessage.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
        await initiator.Internal_AddToConversationAsync(await GetIdAsync());
        await contact.Internal_AddToConversationAsync(await GetIdAsync());
    }

    public async Task InitializeNewGroupConversationAsync(IUserGrain initiator, List<IUserGrain> contacts, IMessageGrain firstMessage)
    {
        _memberIds.State.Add(StringValue.New(await initiator.GetIdAsync()));
        await initiator.Internal_AddToConversationAsync(await GetIdAsync());
        foreach (var contact in contacts)
        {
            _memberIds.State.Add(StringValue.New(await contact.GetIdAsync()));
            await contact.Internal_AddToConversationAsync(await GetIdAsync());
        }
        await _memberIds.WriteStateAsync(); await _communicationIds.AddItemAndPersistAsync(await firstMessage.GetIdAsync());
        await _communicationChangedManager.Notify(sub => sub.OnMessagePosted(firstMessage));
    }

    public async Task<List<IMessageGrain>> GetMessagesAsync(DateTime? datetimeFrom = null, DateTime? datetimeTo = null)
    {
        var messages = new List<IMessageGrain>(75);

        for (int i = 0; i < _communicationIds.State.Count; i++)
        {
            var messageId = _communicationIds.State[i];
            var message = GrainFactory.GetGrain<IMessageGrain>(messageId);
            if (datetimeFrom is { } && await message.GetTimestampAsync() < datetimeFrom) continue;
            if (datetimeTo is { } && await message.GetTimestampAsync() > datetimeTo) continue;
            messages.Add(message);
        }

        return messages;
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