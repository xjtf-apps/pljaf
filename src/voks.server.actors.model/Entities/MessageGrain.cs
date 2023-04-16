using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;

namespace voks.server.model;

public class MessageGrain : Grain, IMessageGrain
{
    private readonly IPersistentState<DateTime> _timestamp;
    private readonly IPersistentState<StringValue> _senderId;
    private readonly IPersistentState<Media?> _mediaReference;
    private readonly IPersistentState<StringValue> _encryptedTextData;

    public MessageGrain(
        [PersistentState(Constants.StoreKeys.Message.Sender)] IPersistentState<StringValue> sender,
        [PersistentState(Constants.StoreKeys.Message.Timestamp)] IPersistentState<DateTime> timestamp,
        [PersistentState(Constants.StoreKeys.Message.EncryptedTextData)] IPersistentState<StringValue> encryptedTextData,
        [PersistentState(Constants.StoreKeys.Message.MediaReference, Constants.Stores.MediaStore)]IPersistentState<Media?> mediaReference)
    {
        _senderId = sender;
        _timestamp = timestamp;
        _mediaReference = mediaReference;
        _encryptedTextData = encryptedTextData;
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<IUserGrain> GetSenderAsync() => await Task.FromResult(GrainFactory.GetGrain<IUserGrain>(_senderId.State.Value));
    public async Task<DateTime> GetTimestampAsync() => await Task.FromResult(_timestamp.State);
    public async Task<Media?> GetMediaReferenceAsync() => await Task.FromResult(_mediaReference.State);
    public async Task<string> GetEncryptedTextDataAsync() => await Task.FromResult(_encryptedTextData.State.Value!);

    public async Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, string encryptedTextData, Media? media)
    {
        _senderId.State = StringValue.New(await sender.GetIdAsync()); await _senderId.WriteStateAsync();
        _timestamp.State = timestamp; await _timestamp.WriteStateAsync();
        _encryptedTextData.State = StringValue.New(encryptedTextData); await _encryptedTextData.WriteStateAsync();
        _mediaReference.State = media; await _mediaReference.WriteStateAsync();
    }
}