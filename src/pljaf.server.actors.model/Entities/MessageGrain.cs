using Orleans;
using Orleans.Runtime;

namespace pljaf.server.model;

public class MessageGrain : Grain, IMessageGrain
{
    private readonly IPersistentState<IUserGrain> _sender;
    private readonly IPersistentState<DateTime> _timestamp;
    private readonly IPersistentState<Media?> _mediaReference;
    private readonly IPersistentState<byte[]> _encryptedTextData;

    public MessageGrain(
        [PersistentState(Constants.StoreKeys.Message.Sender)] IPersistentState<IUserGrain> sender,
        [PersistentState(Constants.StoreKeys.Message.Timestamp)] IPersistentState<DateTime> timestamp,
        [PersistentState(Constants.StoreKeys.Message.MediaReference)]IPersistentState<Media?> mediaReference,
        [PersistentState(Constants.StoreKeys.Message.EncryptedTextData)] IPersistentState<byte[]> encryptedTextData)
    {
        _sender = sender;
        _timestamp = timestamp;
        _mediaReference = mediaReference;
        _encryptedTextData = encryptedTextData;
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<IUserGrain> GetSenderAsync() => await Task.FromResult(_sender.State);
    public async Task<DateTime> GetTimestampAsync() => await Task.FromResult(_timestamp.State);
    public async Task<Media?> GetMediaReferenceAsync() => await Task.FromResult(_mediaReference.State);
    public async Task<byte[]> GetEncryptedTextDataAsync() => await Task.FromResult(_encryptedTextData.State);

    public async Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, byte[] encryptedTextData, Media? mediaReference = null)
    {
        _sender.State = sender; await _sender.WriteStateAsync();
        _timestamp.State = timestamp; await _timestamp.WriteStateAsync();
        _mediaReference.State = mediaReference; await _mediaReference.WriteStateAsync();
        _encryptedTextData.State = encryptedTextData; await _encryptedTextData.WriteStateAsync();
    }
}