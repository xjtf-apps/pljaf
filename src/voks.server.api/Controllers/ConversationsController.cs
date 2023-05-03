using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using voks.server.model;
using voks.client.model;

namespace voks.server.api.Controllers;

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
        var invitations = await currentUser.GetInvitationsAsync()!;
        var invitedConversations = await invitations.ToAsyncEnumerable()
            .SelectAwait(async invite => await invite.GetConversationAsync()).ToListAsync();

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

        return Ok(await conversations.Concat(invitedConversations).ToAsyncEnumerable().WhereAwait(MatchesQuery).SelectAwait(async conv =>
        {
            var id = await conv.GetIdAsync();
            var name = await conv.GetNameAsync();
            var topic = await conv.GetTopicAsync();
            var messages = await conv.GetMessageCountAsync();
            var members = await conv.GetMembersAsync();
            var userIsMember = await members.ToAsyncEnumerable().AnyAwaitAsync(MatchesUser);

            var lastMessage = await conv.GetLastMessageAsync();
            var lastMessageSender = await lastMessage.GetSenderAsync();
            var lastMessageText = await lastMessage.GetEncryptedTextDataAsync();
            var lastMessageMedia = await lastMessage.GetMediaReferenceAsync();

            var lastMessageSenderDisplay =
                (await lastMessageSender.GetProfileAsync()).DisplayName?.Split(' ')[0] ??
                (await lastMessageSender.GetIdAsync());

            var lastMessageContentDisplay =
                lastMessageMedia?.Filename != null ? $"privitak 🖇" : lastMessageText;

            return new
            {
                ConvId = id,
                ConversationName = name,
                ConversationTopic = topic,

                MessageCount = messages,
                LastMessageSender = await CreateUserDtoAsync(lastMessageSender),
                LastMessagePreview = lastMessageContentDisplay,
                LastMessageTimestamp = await lastMessage.GetTimestampAsync(),

                UserIsMember = userIsMember,
                UserIsInvited = !userIsMember,
                Members = members.ToAsyncEnumerable().SelectAwait(CreateUserDtoAsync),

                Kind = (await conv.CheckIsGroupConversationAsync()) ? "Group" : "OneOnOne"
            };
        })
        .ToArrayAsync());
    }

    [HttpPost]
    [Authorize]
    [Route("/conversations/{convId}/message/new")]
    [Route("/conversations/{convId}/message/media/new")]
    public async Task<IActionResult> PostMessage([FromForm] MessageRequest model, [FromRoute] string convId)
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
    [Route("/conversations/{convId}/messages")]
    //[Route("/conversations/{convId}/messages/last/{n}")]
    //[Route("/conversations/{convId}/messages/from/{from}/to/{to}")]
    [Route("/conversations/{convId}/messages/page/{index}/size/{pageSize}")]
    public async Task<IActionResult> GetMessages(string convId, int? index = null, int? pageSize = null)
    {
        var conversationId = Guid.Parse(convId);
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;

        var conversation = await
            (await currentUser.GetConversationsAsync()).ToAsyncEnumerable()
            .WhereAwait(async conv => (await conv.GetIdAsync()) == conversationId)
            .FirstOrDefaultAsync();


        if (conversation == null) return BadRequest("No such conversation found");
        var totalMessageCount = await conversation.GetMessageCountAsync();
        pageSize ??= index == null ? totalMessageCount : 100;
        index ??= 0;

        var messages = await (await conversation.GetMessagesAsync()).ToAsyncEnumerable()
            .Skip((int)index * ((int)pageSize)).Take((int)pageSize)
            .ToListAsync();

        var nextPageIsAvailable =
            ((totalMessageCount / pageSize) - 1) > index;

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
                    MediaRef = mediaRef != null ? new AnyMediaReference { StoreId = mediaRef.StoreId } : null,
                };
            }),
            EarliestMessageTimestamp = await messages.ToAsyncEnumerable().MinAwaitAsync(async m => await m.GetTimestampAsync()),
            LastestMessageTimestamp = await messages.ToAsyncEnumerable().MaxAwaitAsync(async m => await m.GetTimestampAsync()),
            TotalMessageCount = await conversation.GetMessageCountAsync(),

            PageInfo = new {
                Size = (int)pageSize,
                CurrentIndex = (int)index,
                FollowingIndex = nextPageIsAvailable ? index + 1 : null,
            }
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

        var invitation = _grainFactory.GetGrain<IConversationInviteGrain>(Guid.NewGuid())!;
        await invitation.InviteUserAsync(currentUser, targetUser, conversation);
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

        var inviteIds = await currentUser.GetInvitationsAsync()!;
        var invitations = await inviteIds.ToAsyncEnumerable().WhereAwait(async i =>
        {
            var conv = await i.GetConversationAsync();
            var convId = await conv.GetIdAsync();
            var check = convId == id;
            return check;

        }).ToListAsync();

        for (int i = 0; i < invitations.Count; i++)
        {
            var invite = invitations[i];
            await currentUser.ResolveInvitationAsync(invite, decision);
        }

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
    public async Task<IActionResult> InitializeNewConversation([FromForm]ConversationRequest model)
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
    public MediaData? MediaMetadata { get; set; }
    public required string EncryptedTextData { get; set; }
    public required List<string> RequestedMembers { get; set; }
}

public class MessageRequest
{
    public MediaData? MediaMetadata { get; set; }
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