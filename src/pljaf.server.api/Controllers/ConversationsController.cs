using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using pljaf.server.model;
using pljaf.client.model;

namespace pljaf.server.api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ConversationsController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;
        private readonly JwtTokenService _jwtTokenService;

        public ConversationsController(
            IGrainFactory grainFactory,
            JwtTokenService jwtTokenService)
        {
            _grainFactory = grainFactory;
            _jwtTokenService = jwtTokenService;
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
                Inviter = currentUser,
                Invited = targetUser,
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
        public async Task<IActionResult> InitializeNewConversation()
        {
            throw new NotImplementedException();
        }
    }
}
