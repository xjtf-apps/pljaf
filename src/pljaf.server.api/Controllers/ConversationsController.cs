using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using pljaf.server.model;
using pljaf.client.model;

namespace pljaf.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MediaSettingsService _mediaSettingsService;

    public ConversationsController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        MediaSettingsService mediaSettingsService)
    {
        _grainFactory = grainFactory;
        _jwtTokenService = jwtTokenService;
        _mediaSettingsService = mediaSettingsService;
    }

    [HttpGet]
    [Route("/conversations")]
    [Route("/conversations/{convId?}")]
    public async Task<IActionResult> Index(string? convId = null)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var conversations = await currentUser.GetConversationsAsync()!;

        async ValueTask<bool> MatchesUser(IUserGrain someUser)
        {
            var someUserId = await someUser.GetIdAsync();
            var query = currentUserId == someUserId;
            return query;
        }

        async ValueTask<bool> MatchesQuery(IConversationGrain conversation)
        {
            var targetId = convId == null ? null : (Guid?)Guid.Parse(convId);
            var query = targetId == null || (await conversation.GetIdAsync()) == targetId;
            return query;
        }

        async ValueTask<User> CreateUserDtoAsync(IUserGrain user)
        {
            var avatar = await user.GetAvatarAsync();

            return new User
            {
                Id = new UserId(await user.GetIdAsync()),
                DisplayName = (await user.GetProfileAsync())?.DisplayName,
                StatusLine = (await user.GetProfileAsync())?.StatusLine,
                AvatarRef = avatar != null ? new ImageRef() { StoreId = avatar.StoreId } : null,
            };
        }

        return Ok(await conversations.ToAsyncEnumerable().WhereAwait(MatchesQuery).SelectAwait(async conv =>
        {
            var id = await conv.GetIdAsync();
            var name = await conv.GetNameAsync();
            var topic = await conv.GetTopicAsync();
            var messages = await conv.GetMessageCountAsync();
            var members = await conv.GetMembersAsync();
            var invitedMembers = await conv.GetInvitedMembersAsync();

            return new
            {
                ConvId = id,
                ConversationName = name,
                ConversationTopic = topic,

                MessageCount = messages,

                Members = members.ToAsyncEnumerable().SelectAwait(CreateUserDtoAsync),
                InvitedMembers = invitedMembers.ToAsyncEnumerable().SelectAwait(CreateUserDtoAsync),

                UserIsMember = await members.ToAsyncEnumerable().AnyAwaitAsync(MatchesUser),
                UserIsInvited = await invitedMembers.ToAsyncEnumerable().AnyAwaitAsync(MatchesUser),

                Kind = (await conv.CheckIsGroupConversationAsync()) ? "Group" : "OneOnOne"
            };
        })
        .ToArrayAsync());
    }

    [HttpPost]
    [Authorize]
    [Route("/conversations/{convId}/message/new")]
    [Route("/conversations/{convId}/message/media/new")]
    public async Task<IActionResult> PostMessage([FromBody] MessageRequest model, [FromRoute] string convId)
    {
        var msgId = Guid.NewGuid();
        var mediaData = model.MediaMetadata;
        var conversationId = Guid.Parse(convId);
        var message = _grainFactory.GetGrain<IMessageGrain>(msgId);
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == conversationId)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");
        if (!CheckMediaPayloadSize(mediaData)) return BadRequest("Media file size too large");

        var media = await ExtractMediaPayload(mediaData);
        await message.AuthorMessageAsync(currentUser, DateTime.UtcNow, model.EncryptedTextData, media);
        await conversation.PostMessageAsync(message);

        return Ok(new
        {
            ConvId = new ConvId(conversationId),
            MsgId = new MsgId(msgId)
        });
    }

    [HttpGet]
    [Authorize]
    [Route("/conversation/{convId}/messages")]
    [Route("/conversation/{convId}/messages/last/{n}")]
    [Route("/conversation/{convId}/messages/from/{from}/to/{to}")]
    public async Task<IActionResult> GetMessages(string convId, DateTime? from = null, DateTime? to = null, int? n = null)
    {
        var conversationId = Guid.Parse(convId);
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == conversationId)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");
        n ??= await conversation.GetMessageCountAsync();

        var messages = await (await conversation.GetMessagesAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => from == null || (await m.GetTimestampAsync()) > from)
            .WhereAwait(async m => to == null || (await m.GetTimestampAsync()) < to)
            .TakeLast((int)n).ToListAsync();

        return Ok(new
        {
            ConvId = new ConvId(conversationId),
            SelectedMessages = messages.ToAsyncEnumerable().SelectAwait(async msg =>
            {
                var mediaRef = await msg.GetMediaReferenceAsync();

                return new
                {
                    Timestamp = await msg.GetTimestampAsync(),
                    EncryptedTextData = await msg.GetEncryptedTextDataAsync(),
                    Sender = new UserId(await (await msg.GetSenderAsync()).GetIdAsync()),
                    MediaRef = mediaRef != null ? new AnyMediaReference { StoreId =  mediaRef.StoreId } : null,
                };
            }),
            EarliestMessageTimestamp = await messages.ToAsyncEnumerable().MinAwaitAsync(async m => await m.GetTimestampAsync()),
            LastestMessageTimestamp = await messages.ToAsyncEnumerable().MaxAwaitAsync(async m => await m.GetTimestampAsync()),
            SelectedMessagesCount = messages.Count,
            TotalMessageCount = await conversation.GetMessageCountAsync()
        });
    }

    [HttpPut]
    [Authorize]
    [Route("/conversations/{convId}/name/{name}")]
    public async Task<IActionResult> SetConversationName(string convId, string name)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var id = Guid.Parse(convId);

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == id)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");
        if (name == null) return BadRequest("Name cannot be null");
        await conversation.SetNameAsync(name);

        return Ok();
    }

    [HttpPut]
    [Authorize]
    [Route("/conversations/{convId}/topic/{topic}")]
    public async Task<IActionResult> SetConversationTopic(string convId, string topic)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var id = Guid.Parse(convId);

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == id)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");
        if (topic == null) return BadRequest("Topic cannot be null");
        await conversation.SetTopicAsync(topic);

        return Ok();
    }

    [HttpPut]
    [Authorize]
    [Route("/conversations/{convId}/invite/{userId}")]
    public async Task<IActionResult> InviteToConversation(string convId, string userId)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var targetUser = _grainFactory.GetGrain<IUserGrain>(userId)!;
        var targetUserId = await targetUser.GetIdAsync()!;
        var id = Guid.Parse(convId);

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == id)
            .FirstOrDefaultAsync();
        if (conversation == null) return BadRequest("No such conversation found");

        var currentMembers = await conversation.GetMembersAsync();
        var currentInvites = await conversation.GetInvitedMembersAsync();
        var current = currentMembers.Concat(currentInvites);

        var query = await current.ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == targetUserId)
            .CountAsync();
        if (query != 0) return BadRequest("User already a member or invited to conversation");

        await conversation.InviteToConversationAsync(new Invitation()
        {
            InviterId = await currentUser.GetIdAsync(),
            InvitedId = await targetUser.GetIdAsync(),
            Timestamp = DateTime.UtcNow
        });

        return Ok();
    }

    [HttpPut]
    [Authorize]
    [Route("/conversations/{convId}/invite/resolve/{decision}")]
    public async Task<IActionResult> ResolveInvitation(string convId, bool decision)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var id = Guid.Parse(convId);

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == id)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");

        var invitation = await conversation.GetInvitationAsync(currentUser);
        if (invitation == null) return BadRequest("No such invitation found");
        await conversation.ResolveInvitationAsync(invitation, decision);

        return Ok();
    }

    [Authorize]
    [HttpDelete]
    [Route("/conversations/{convId}/leave")]
    public async Task<IActionResult> LeaveConversation(string convId)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var id = Guid.Parse(convId);

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == id)
            .FirstOrDefaultAsync();

        if (conversation == null) return BadRequest("No such conversation found");
        await conversation.LeaveConversationAsync(currentUser);
        return NoContent();
    }

    [HttpPost]
    [Authorize]
    [Route("/conversations/new")]
    public async Task<IActionResult> InitializeNewConversation([FromBody]ConversationRequest model)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var message = _grainFactory.GetGrain<IMessageGrain>(Guid.NewGuid());
        var mediaData = model.MediaMetadata;
        var convId = Guid.NewGuid();

        if (!CheckMediaPayloadSize(mediaData))
        {
            return BadRequest("Media file size too large");
        }
        var media = await ExtractMediaPayload(mediaData);

        await message.AuthorMessageAsync
            (currentUser, DateTime.UtcNow, model.EncryptedTextData, media);

        var members = model.RequestedMembers.Select(m =>
        {
            return _grainFactory.GetGrain<IUserGrain>(m)!;

        }).ToList();

        var conversation = _grainFactory.GetGrain<IConversationGrain>(convId);
        var conversationType = model.RequestedMembers.Count == 1 ? "OneOnOne" : "Group";

        if (conversationType == "OneOnOne")
            await conversation.InitializeNewConversationAsync(currentUser, members.First(), message);
        if (conversationType == "Group")
            await conversation.InitializeNewGroupConversationAsync(currentUser, members, message);

        if (model.Name is { })
            await conversation.SetNameAsync(model.Name!);
        if (model.Topic is { })
            await conversation.SetTopicAsync(model.Topic!);

        return Ok(new
        {
            ConvId = new ConvId(convId),
            MessageId = new MsgId(await message.GetIdAsync())
        });
    }

    private async Task<Media?> ExtractMediaPayload(MediaData? mediaData)
    {
        if (mediaData == null) return null;
        using var writeStream = new MemoryStream();
        using var readStream = mediaData.MediaDataTransfer.OpenReadStream();
        await readStream.CopyToAsync(writeStream);

        return new()
        {
            StoreId = Guid.NewGuid(),
            BinaryData = writeStream.ToArray(),
            Filename = mediaData.MediaDataTransfer.FileName
        };
    }

    private bool CheckMediaPayloadSize(MediaData? mediaData)
    {
        if (mediaData == null) return true;
        var mediaDataType = mediaData.MediaType;
        var mediaSize = mediaData.MediaDataTransfer.Length;
        if (mediaDataType == MediaDataType.Image && mediaSize > _mediaSettingsService.MaxImageSize) return false;
        if (mediaDataType == MediaDataType.Audio && mediaSize > _mediaSettingsService.MaxAudioSize) return false;
        if (mediaDataType == MediaDataType.Video && mediaSize > _mediaSettingsService.MaxVideoSize) return false;
        return true;
    }
}

public class ConversationRequest
{
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public required List<string> RequestedMembers { get; set; }
    public required MediaData? MediaMetadata { get; set; }
    public required string EncryptedTextData { get; set; }
}

public class MessageRequest
{
    public required MediaData? MediaMetadata { get; set; }
    public required string EncryptedTextData { get; set; }
}

public class MediaData
{
    public required MediaDataType MediaType { get; set; }
    public required IFormFile MediaDataTransfer { get; set; }
}

public enum MediaDataType
{
    Image, Audio, Video
}