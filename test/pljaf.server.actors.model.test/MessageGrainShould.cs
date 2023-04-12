namespace pljaf.server.model.test;

[Collection(ClusterCollection.Name)]
public class MessageGrainShould
{
    #region test class
    private readonly TestCluster _cluster;
    public IGrainFactory GrainFactory => _cluster.GrainFactory;
    public MessageGrainShould(ClusterFixture fixture) => _cluster = fixture.Cluster;
    #endregion

    [Fact]
    public async Task GetCorrectId()
    {
        var msgId = Guid.NewGuid();
        var msgGrain = GrainFactory.GetGrain<IMessageGrain>(msgId);
        var msgIdRetrieved = await msgGrain.GetIdAsync();

        Assert.Equal(msgId, msgIdRetrieved);
    }

    [Fact]
    public async Task GetCorrectMetadata()
    {
        var msgId = Guid.NewGuid();
        var msgTs = DateTime.UtcNow;
        var userPhone = "+385976133776";
        var msgGrain = GrainFactory.GetGrain<IMessageGrain>(msgId);
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        await msgGrain.AuthorMessageAsync(userGrain, msgTs, "Hello World!");

        var sender = await msgGrain.GetSenderAsync();
        var timestamp = await msgGrain.GetTimestampAsync();
        var content = await msgGrain.GetEncryptedTextDataAsync();

        Assert.NotNull(sender);
        Assert.Equal(msgTs, timestamp);
        Assert.Equal("Hello World!", content);
        Assert.Equal(userPhone, await sender.GetIdAsync());
    }

    [Fact]
    public async Task GetCorrectMedia()
    {

    }
}
