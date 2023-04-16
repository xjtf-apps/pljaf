namespace voks.server.model.test;


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
    public async Task GetCorrectDataAndMetadata()
    {
        var msgId = Guid.NewGuid();
        var msgTs = DateTime.UtcNow;
        var userPhone = "+3851111222";
        var msgGrain = GrainFactory.GetGrain<IMessageGrain>(msgId);
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userPhone);
        var msgMedia = new Media() { StoreId = Guid.NewGuid(), Filename = "file.ext", BinaryData = new byte[] { 0xAA, 0xBC } };

        await msgGrain.AuthorMessageAsync
            (userGrain, msgTs, "Hello World!", msgMedia);

        var tasks = new Task[]
        {
            msgGrain.GetSenderAsync(),
            msgGrain.GetTimestampAsync(),
            msgGrain.GetMediaReferenceAsync(),
            msgGrain.GetEncryptedTextDataAsync()
        };
        await Task.WhenAll(tasks);
        var sender = ((Task<IUserGrain>)tasks[0]).Result;
        var timestamp = ((Task<DateTime>)tasks[1]).Result;
        var media = ((Task<Media?>)tasks[2]).Result;
        var content = ((Task<string>)tasks[3]).Result;

        Assert.NotNull(sender);
        Assert.Equal(msgTs, timestamp);
        Assert.Equal("Hello World!", content);
        Assert.Equal(userPhone, await sender.GetIdAsync());
        Assert.NotNull(media);
        Assert.Equal(msgMedia.StoreId, media.StoreId);
        Assert.Equal(msgMedia.Filename, media.Filename);
        Assert.Equal(msgMedia.BinaryData, media.BinaryData);
    }
}
