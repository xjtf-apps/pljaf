using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using pljaf.server.model;
using pljaf.client.model;

namespace pljaf.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class MediaController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MediaSettingsService _mediaSettingsService;

    public MediaController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        MediaSettingsService mediaSettingsService)
    {
        _grainFactory = grainFactory;
        _jwtTokenService = jwtTokenService;
        _mediaSettingsService = mediaSettingsService;
    }

    [HttpPost]
    [Authorize]
    [Route("/media/attach/image/{msgId}/{convId}")]
    public async Task<IActionResult> AttachImageMediaToMessage([FromBody] IFormFile imageMedia, [FromRoute] Guid msgId, [FromRoute] Guid convId)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var conversation = _grainFactory.GetGrain<IConversationGrain>(convId)!;
        var message = _grainFactory.GetGrain<IMessageGrain>(msgId)!;

        var messageInConversation = await
            (await conversation.GetMessagesAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == msgId)
            .FirstOrDefaultAsync() != null;

        var userInConversation = await
            (await conversation.GetMembersAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == currentUserId)
            .FirstOrDefaultAsync() != null;

        if (userInConversation && messageInConversation)
        {
            var mediaStoreId = Guid.NewGuid();
            var maxSize = _mediaSettingsService.MaxImageSize;
            if (imageMedia.Length > maxSize) return BadRequest("File size too large");

            using var readStream = imageMedia.OpenReadStream();
            using var outputStream = new MemoryStream();
            await readStream.CopyToAsync(outputStream);

            await message.SetMediaReferenceAsync(new Media()
            {
                StoreId = mediaStoreId,
                Filename = imageMedia.FileName,
                BinaryData = outputStream.ToArray()
            });

            return Ok(new
            {
                ConvId = new ConvId(convId),
                MsgId = new MsgId(msgId),
                ImageRef = new ImageRef() { StoreId = mediaStoreId }
            });
        }
        else return BadRequest("User or message not in conversation");
    }

    [HttpPost]
    [Authorize]
    [Route("media/attach/video/{msgId}")]
    public async Task<IActionResult> AttachVideoMediaToMessage([FromBody] IFormFile videoMedia, [FromRoute] Guid msgId, [FromRoute] Guid convId)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var conversation = _grainFactory.GetGrain<IConversationGrain>(convId)!;
        var message = _grainFactory.GetGrain<IMessageGrain>(msgId)!;

        var messageInConversation = await
            (await conversation.GetMessagesAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == msgId)
            .FirstOrDefaultAsync() != null;

        var userInConversation = await
            (await conversation.GetMembersAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == currentUserId)
            .FirstOrDefaultAsync() != null;

        if (userInConversation && messageInConversation)
        {
            var mediaStoreId = Guid.NewGuid();
            var maxSize = _mediaSettingsService.MaxVideoSize;
            if (videoMedia.Length > maxSize) return BadRequest("File size too large");

            using var readStream = videoMedia.OpenReadStream();
            using var outputStream = new MemoryStream();
            await readStream.CopyToAsync(outputStream);

            await message.SetMediaReferenceAsync(new Media()
            {
                StoreId = mediaStoreId,
                Filename = videoMedia.FileName,
                BinaryData = outputStream.ToArray()
            });

            return Ok(new
            {
                ConvId = new ConvId(convId),
                MsgId = new MsgId(msgId),
                VideoRef = new VideoRef() { StoreId = mediaStoreId }
            });
        }
        else return BadRequest("User or message not in conversation");
    }

    [HttpPost]
    [Authorize]
    [Route("/media/attach/audio/{msgId}")]
    public async Task<IActionResult> AttachAudioMediaToMessage([FromBody] IFormFile audioMedia, [FromRoute] Guid msgId, [FromRoute] Guid convId)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext)!;
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId)!;
        var conversation = _grainFactory.GetGrain<IConversationGrain>(convId)!;
        var message = _grainFactory.GetGrain<IMessageGrain>(msgId)!;

        var messageInConversation = await
            (await conversation.GetMessagesAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == msgId)
            .FirstOrDefaultAsync() != null;

        var userInConversation = await
            (await conversation.GetMembersAsync()).ToAsyncEnumerable()
            .WhereAwait(async m => (await m.GetIdAsync()) == currentUserId)
            .FirstOrDefaultAsync() != null;

        if (userInConversation && messageInConversation)
        {
            var mediaStoreId = Guid.NewGuid();
            var maxSize = _mediaSettingsService.MaxAudioSize;
            if (audioMedia.Length > maxSize) return BadRequest("File size too large");

            using var readStream = audioMedia.OpenReadStream();
            using var outputStream = new MemoryStream();
            await readStream.CopyToAsync(outputStream);

            await message.SetMediaReferenceAsync(new Media()
            {
                StoreId = mediaStoreId,
                Filename = audioMedia.FileName,
                BinaryData = outputStream.ToArray()
            });

            return Ok(new
            {
                ConvId = new ConvId(convId),
                MsgId = new MsgId(msgId),
                AudioRef = new AudioRef() { StoreId = mediaStoreId }
            });
        }
        else return BadRequest("User or message not in conversation");
    }
}
