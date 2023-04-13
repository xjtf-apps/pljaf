namespace pljaf.server.model.test;

[Collection(ClusterCollection.Name)]
public class ConversationGrainShould
{
    #region test class
    private readonly TestCluster _cluster;
    public IGrainFactory GrainFactory => _cluster.GrainFactory;
    public ConversationGrainShould(ClusterFixture fixture) => _cluster = fixture.Cluster;
    #endregion

    [Fact]
    public async Task GetCorrectId()
    {
        var convId = Guid.NewGuid();
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);
        var convIdRetrieved = await conversation.GetIdAsync();

        Assert.Equal(convId, convIdRetrieved);
    }

    [Fact]
    public async Task GetCorrectName()
    {
        var convId = Guid.NewGuid();
        var convName = "Happy participants chat";
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);

        await conversation.SetNameAsync(convName);
        var nameRetrieved = await conversation.GetNameAsync();

        Assert.NotNull(nameRetrieved);
        Assert.Equal(convName, nameRetrieved);
    }

    [Fact]
    public async Task GetCorrectTopic()
    {
        var convId = Guid.NewGuid();
        var convTopic = "Rather cheerful topic";
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);

        await conversation.SetTopicAsync(convTopic);
        var topicRetrieved = await conversation.GetTopicAsync();

        Assert.NotNull(topicRetrieved);
        Assert.Equal(convTopic, topicRetrieved);
    }

    [Fact]
    public async Task GetCorrectMessageCount()
    {
        var convId = Guid.NewGuid();
        var sender = GrainFactory.GetGrain<IUserGrain>("+3851111222");
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);
        var messageIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        Assert.Equal(0, await conversation.GetMessageCountAsync());

        for (int i = 0; i < messageIds.Length; i++)
        {
            var messageGrain = GrainFactory.GetGrain<IMessageGrain>(messageIds[i]);
            await messageGrain.AuthorMessageAsync(sender, DateTime.UtcNow, "Hello", null);
            await conversation.PostMessageAsync(messageGrain);

            Assert.Equal(i + 1, await conversation.GetMessageCountAsync());
        }
    }

    [Fact]
    public async Task GetCorrectConversationType()
    {
        var conv1Id = Guid.NewGuid(); var conv2Id = Guid.NewGuid();
        var message = GrainFactory.GetGrain<IMessageGrain>(Guid.NewGuid());
        var participant1 = GrainFactory.GetGrain<IUserGrain>("+3851111222");
        var participant2 = GrainFactory.GetGrain<IUserGrain>("+3852222333");
        var participant3 = GrainFactory.GetGrain<IUserGrain>("+3853333444");
        var conversation1 = GrainFactory.GetGrain<IConversationGrain>(conv1Id);
        var conversation2 = GrainFactory.GetGrain<IConversationGrain>(conv2Id);

        await conversation1.InitializeNewConversationAsync(participant1, participant2, message);
        await conversation2.InitializeNewGroupConversationAsync(participant1, new List<IUserGrain>() { participant2, participant3 }, message);

        Assert.False(await conversation1.CheckIsGroupConversationAsync());
        Assert.True(await conversation2.CheckIsGroupConversationAsync());
    }

    [Fact]
    public async Task GetCorrectMembers()
    {
        var conv1Id = Guid.NewGuid(); var conv2Id = Guid.NewGuid();
        var message = GrainFactory.GetGrain<IMessageGrain>(Guid.NewGuid());
        var participant1 = GrainFactory.GetGrain<IUserGrain>("+3851111222");
        var participant2 = GrainFactory.GetGrain<IUserGrain>("+3852222333");
        var participant3 = GrainFactory.GetGrain<IUserGrain>("+3853333444");
        var conversation1 = GrainFactory.GetGrain<IConversationGrain>(conv1Id);
        var conversation2 = GrainFactory.GetGrain<IConversationGrain>(conv2Id);

        await conversation1.InitializeNewConversationAsync(participant1, participant2, message);
        await conversation2.InitializeNewGroupConversationAsync(participant1, new List<IUserGrain>() { participant2, participant3 }, message);

        var members1 = await conversation1.GetMembersAsync();
        var members2 = await conversation2.GetMembersAsync();

        Assert.Equal(2, members1.Count);
        Assert.Equal(3, members2.Count);
        Assert.Contains(
            await participant1.GetIdAsync(),
            await members1.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant2.GetIdAsync(),
            await members1.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.DoesNotContain(
            await participant3.GetIdAsync(),
            await members1.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant1.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant2.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant3.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());

        await conversation2.LeaveConversationAsync(participant3);
        members2 = await conversation2.GetMembersAsync();

        Assert.Equal(2, members2.Count);
        Assert.Contains(
            await participant1.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant2.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.DoesNotContain(
            await participant3.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());

        var invitation = new Invitation()
        {
            InviterId = await participant1.GetIdAsync(),
            InvitedId = await participant3.GetIdAsync(),
            Timestamp = DateTime.UtcNow
        };
        await conversation2.InviteToConversationAsync(invitation);
        await conversation2.ResolveInvitationAsync(invitation, accepted: true);
        members2 = await conversation2.GetMembersAsync();

        Assert.Equal(3, members2.Count);
        Assert.Contains(
            await participant1.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant2.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await participant3.GetIdAsync(),
            await members2.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
    }

    [Fact]
    public async Task GetCorrectInvitees()
    {
        var convId = Guid.NewGuid();
        var message = GrainFactory.GetGrain<IMessageGrain>(Guid.NewGuid());
        var member1 = GrainFactory.GetGrain<IUserGrain>("+3851111222");
        var member2 = GrainFactory.GetGrain<IUserGrain>("+3852222333");
        var invitee1 = GrainFactory.GetGrain<IUserGrain>("+3853333444");
        var invitee2 = GrainFactory.GetGrain<IUserGrain>("+3854444555");
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);
        await conversation.InitializeNewConversationAsync(member1, member2, message);

        await conversation.InviteToConversationAsync(new Invitation()
        {
            InviterId = await member1.GetIdAsync(),
            InvitedId = await invitee1.GetIdAsync(),
            Timestamp = DateTime.UtcNow
        });

        var invited = await conversation.GetInvitedMembersAsync();

        Assert.NotNull(invited);
        Assert.Single(invited);
        Assert.Contains(
            await invitee1.GetIdAsync(),
            await invited.ToAsyncEnumerable().SelectAwait(async i => await i.GetIdAsync()).ToListAsync());

        var invitee1_invitation = await conversation.GetInvitationAsync(invitee1);
        Assert.NotNull(invitee1_invitation);
        await conversation.ResolveInvitationAsync(invitee1_invitation, accepted: true);

        invited = await conversation.GetInvitedMembersAsync();

        Assert.NotNull(invited);
        Assert.Empty(invited);
        Assert.DoesNotContain(
            await invitee1.GetIdAsync(),
            await invited.ToAsyncEnumerable().SelectAwait(async i => await i.GetIdAsync()).ToListAsync());

        await conversation.InviteToConversationAsync(new Invitation()
        {
            InviterId = await member1.GetIdAsync(),
            InvitedId = await invitee2.GetIdAsync(),
            Timestamp = DateTime.UtcNow
        });

        invited = await conversation.GetInvitedMembersAsync();

        Assert.NotNull(invited);
        Assert.Single(invited);
        Assert.Contains(
            await invitee2.GetIdAsync(),
            await invited.ToAsyncEnumerable().SelectAwait(async i => await i.GetIdAsync()).ToListAsync());

        var invitee2_invitation = await conversation.GetInvitationAsync(invitee2);
        Assert.NotNull(invitee2_invitation);
        await conversation.ResolveInvitationAsync(invitee2_invitation, accepted: false);

        invited = await conversation.GetInvitedMembersAsync();

        Assert.NotNull(invited);
        Assert.Empty(invited);
        Assert.DoesNotContain(
            await invitee2.GetIdAsync(),
            await invited.ToAsyncEnumerable().SelectAwait(async i => await i.GetIdAsync()).ToListAsync());

        var members = await conversation.GetMembersAsync();
        Assert.NotNull(members);
        Assert.NotEmpty(members);
        Assert.Equal(3, members.Count);
        Assert.Contains(
            await member1.GetIdAsync(),
            await members.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await member2.GetIdAsync(),
            await members.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await invitee1.GetIdAsync(),
            await members.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
        Assert.DoesNotContain(
            await invitee2.GetIdAsync(),
            await members.ToAsyncEnumerable().SelectAwait(async m => await m.GetIdAsync()).ToListAsync());
    }

    [Fact]
    public async Task GetCorrectMessages()
    {
        var now = DateTime.UtcNow;
        var convId = Guid.NewGuid();
        var sender = GrainFactory.GetGrain<IUserGrain>("+3851112222");
        var conversation = GrainFactory.GetGrain<IConversationGrain>(convId);
        var messageIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        for (int i = 0; i < messageIds.Length; i++)
        {
            var messageId = messageIds[i];
            var messageTs = now.AddMinutes(i * 10);
            var messageGrain = GrainFactory.GetGrain<IMessageGrain>(messageId);
            await messageGrain.AuthorMessageAsync(sender, messageTs, $"Test message {i + 1}", null);

            await conversation.PostMessageAsync(messageGrain);
        }

        var allMessages = await conversation.GetMessagesAsync();
        var allMessagesCount = await conversation.GetMessageCountAsync();
        Assert.NotNull(allMessages);
        Assert.Equal(3, allMessages.Count);
        Assert.Equal(allMessagesCount, allMessages.Count);
        Assert.Equal("Test message 1", await allMessages[0].GetEncryptedTextDataAsync());
        Assert.Equal("Test message 2", await allMessages[1].GetEncryptedTextDataAsync());
        Assert.Equal("Test message 3", await allMessages[2].GetEncryptedTextDataAsync());

        var firstMessage = await conversation.GetMessagesAsync(now, now.AddMinutes(5));
        Assert.NotNull(firstMessage);
        Assert.Single(firstMessage);
        Assert.Equal("Test message 1", await firstMessage[0].GetEncryptedTextDataAsync());

        var firstTwoMessages = await conversation.GetMessagesAsync(now, now.AddMinutes(15));
        Assert.NotNull(firstTwoMessages);
        Assert.Equal(2, firstTwoMessages.Count);
        Assert.Equal("Test message 1", await firstTwoMessages[0].GetEncryptedTextDataAsync());
        Assert.Equal("Test message 2", await firstTwoMessages[1].GetEncryptedTextDataAsync());
    }
}
