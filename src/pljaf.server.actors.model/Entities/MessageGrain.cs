using Orleans;
using Orleans.Runtime;

namespace pljaf.server.model;

public class MessageGrain : Grain, IMessageGrain
{
    public Guid Id => this.GetGrainId().GetGuidKey();

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

    public IUserGrain Sender => _sender.State;
    public DateTime Timestamp => _timestamp.State;
    public Media? MediaReference => _mediaReference.State;
    public byte[] EncryptedTextData => _encryptedTextData.State;

    public async Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, byte[] encryptedTextData, Media? mediaReference = null)
    {
        _sender.State = sender; await _sender.WriteStateAsync();
        _timestamp.State = timestamp; await _timestamp.WriteStateAsync();
        _mediaReference.State = mediaReference; await _mediaReference.WriteStateAsync();
        _encryptedTextData.State = encryptedTextData; await _encryptedTextData.WriteStateAsync();
    }
}