using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using pljaf.server.model;
using System.Text;

namespace pljaf.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ClientController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtService;
    private readonly IForwardedClientObserver _clientObserver;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private string? CurrentUserId => _jwtService.GetUserIdFromRequest(HttpContext);
    private IUserGrain CurrentUser => _grainFactory.GetGrain<IUserGrain>(CurrentUserId);
    private CancellationToken ApplicationStopping => _applicationLifetime.ApplicationStopping;

    public ClientController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        IForwardedClientObserver clientObserver,
        IHostApplicationLifetime applicationLifetime)
    {
        _grainFactory = grainFactory;
        _jwtService = jwtTokenService;
        _clientObserver = clientObserver;
        _applicationLifetime = applicationLifetime;
    }

    [HttpGet]
    [Authorize]
    [Route("/client/subscribe")]
    public async Task Subscribe()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var messages = new List<string>();
            var conversations = await CurrentUser.GetConversationsAsync()!;
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // subscribe to grains,
            // emit messages to list above,
            // the web socket task will consume the messages

            var webSocketTask = Task.Run(async () =>
            {
                while (true)
                {
                    await SendAsync(webSocket, messages);
                    await Task.Delay(1_000);
                }
            
            }, ApplicationStopping);

            await webSocketTask;
            await CloseAsync(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task SendAsync(WebSocket webSocket, List<string> messages)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            var messageType = WebSocketMessageType.Text;
            var messageContent = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(messageContent, messageType, true, ApplicationStopping);
        }
    }

    private async Task CloseAsync(WebSocket webSocket)
    {
        var status = WebSocketCloseStatus.EndpointUnavailable;
        await webSocket.CloseAsync(status, null, ApplicationStopping);
    }
}
