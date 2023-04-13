namespace pljaf.server.model.test;

[Collection(ClusterCollection.Name)]
public class UserGrainShould
{
    #region test class
    private readonly TestCluster _cluster;
    public IGrainFactory GrainFactory => _cluster.GrainFactory;
    public UserGrainShould(ClusterFixture fixture) => _cluster = fixture.Cluster;
    #endregion

    [Fact]
    public async Task GetCorrectId()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        var userGrainId = await userGrain.GetIdAsync();

        Assert.Equal(userPhone, userGrainId);
    }

    [Fact]
    public async Task GetCorrectProfile()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);

        var userProfileConfigured = new Profile()
        {
            DisplayName = "Filip Vukovinski",
            StatusLine = "unit testing pljaf grains",
            ProfilePicture = new Media()
            {
                StoreId = Guid.NewGuid(),
                Filename = "myprofilepic.png",
                BinaryData = new byte[] { 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF }
            }
        };
        await userGrain.SetProfileAsync(userProfileConfigured);
        var userProfileRertrieved = await userGrain.GetProfileAsync();

        Assert.NotNull(userProfileRertrieved);
        Assert.Equal(userProfileConfigured.DisplayName, userProfileRertrieved.DisplayName);
        Assert.Equal(userProfileConfigured.StatusLine, userProfileRertrieved.StatusLine);
        Assert.Equal(userProfileConfigured.ProfilePicture.StoreId, userProfileRertrieved.ProfilePicture?.StoreId);
        Assert.Equal(userProfileConfigured.ProfilePicture.Filename, userProfileRertrieved.ProfilePicture?.Filename);
        Assert.Equal(userProfileConfigured.ProfilePicture.BinaryData, userProfileRertrieved.ProfilePicture?.BinaryData);
    }

    [Fact]
    public async Task GetCorrectOptions()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);

        var userOptionsConfigured = new Options()
        {
            SendNotifications = true
        };
        await userGrain.SetOptionsAsync(userOptionsConfigured);
        var userOptionsRetrieved = await userGrain.GetOptionsAsync();

        Assert.NotNull(userOptionsRetrieved);
        Assert.Equal(userOptionsConfigured.SendNotifications, userOptionsRetrieved.SendNotifications);
    }

    [Fact]
    public async Task GetCorrectContacts()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        var userContacts = new[] { "+385910000000", "+385951111222" }.Select(ph =>
        {
            return GrainFactory.GetGrain<IUserGrain>(ph);

        }).ToList();

        await userGrain.AddContactAsync(userContacts[0]);
        await userGrain.AddContactAsync(userContacts[1]);
        var userContactsRetrieved = await userGrain.GetContactsAsync();

        Assert.NotNull(userContactsRetrieved);
        Assert.Equal(2, userContactsRetrieved.Count);
        Assert.Contains(
            await userContacts[0].GetIdAsync(),
            await userContactsRetrieved.ToAsyncEnumerable().SelectAwait(async c => await c.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await userContacts[1].GetIdAsync(),
            await userContactsRetrieved.ToAsyncEnumerable().SelectAwait(async c => await c.GetIdAsync()).ToListAsync());

        await userGrain.RemoveContactAsync(userContacts[0]);
        userContactsRetrieved = await userGrain.GetContactsAsync();
        Assert.NotNull(userContactsRetrieved);
        Assert.Single(userContactsRetrieved);
        Assert.DoesNotContain(
            await userContacts[0].GetIdAsync(),
            await userContactsRetrieved.ToAsyncEnumerable().SelectAwait(async c => await c.GetIdAsync()).ToListAsync());
        Assert.Contains(
            await userContacts[1].GetIdAsync(),
            await userContactsRetrieved.ToAsyncEnumerable().SelectAwait(async c => await c.GetIdAsync()).ToListAsync());
    }

    [Fact]
    public async Task GetCorrectTokens()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        var userTokensConfigured = new Tokens()
        {
            AccessToken = "secret-access-token",
            RefreshToken = "secret-refresh-token",
            RefreshTokenExpires = DateTime.UtcNow.AddMinutes(40)
        };

        await userGrain.SetTokensAsync(userTokensConfigured);
        var userTokensRetrieved = await userGrain.GetTokensAsync();

        Assert.NotNull(userTokensRetrieved);
        Assert.Equal(userTokensConfigured.AccessToken, userTokensRetrieved.AccessToken);
        Assert.Equal(userTokensConfigured.RefreshToken, userTokensRetrieved.RefreshToken);
        Assert.Equal(userTokensConfigured.RefreshTokenExpires, userTokensRetrieved.RefreshTokenExpires);
    }

    [Fact]
    public async Task GetCorrectConversations()
    {
        var userPhone = "+3851111222";
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        var userConversations = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await userGrain.Internal_AddToConversationAsync(userConversations[0]);
        await userGrain.Internal_AddToConversationAsync(userConversations[1]);
        var userConversationsRetrieved = await userGrain.GetConversationsAsync();
        var userConversationCountRetrieved = await userGrain.GetConversationsCountAsync();

        Assert.NotNull(userConversationsRetrieved);
        Assert.Equal(2, userConversationCountRetrieved);
        Assert.Equal(userConversationCountRetrieved, userConversationsRetrieved.Count);
        Assert.Contains(
            userConversations[0],
            await userConversationsRetrieved.ToAsyncEnumerable().SelectAwait(async conv => await conv.GetIdAsync()).ToListAsync());
        Assert.Contains(
            userConversations[1],
            await userConversationsRetrieved.ToAsyncEnumerable().SelectAwait(async conv => await conv.GetIdAsync()).ToListAsync());

        await userGrain.Internal_RemoveFromConversationAsync(userConversations[0]);
        userConversationsRetrieved = await userGrain.GetConversationsAsync();
        userConversationCountRetrieved = await userGrain.GetConversationsCountAsync();

        Assert.NotNull(userConversationsRetrieved);
        Assert.Equal(1, userConversationCountRetrieved);
        Assert.Single(userConversationsRetrieved);
        Assert.DoesNotContain(
            userConversations[0],
            await userConversationsRetrieved.ToAsyncEnumerable().SelectAwait(async conv => await conv.GetIdAsync()).ToListAsync());
        Assert.Contains(
            userConversations[1],
            await userConversationsRetrieved.ToAsyncEnumerable().SelectAwait(async conv => await conv.GetIdAsync()).ToListAsync());
    }
}
