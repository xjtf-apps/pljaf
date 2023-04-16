using System.Text;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using voks.server.model;

namespace voks.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ClientController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtService;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private string? CurrentUserId => _jwtService.GetUserIdFromRequest(HttpContext);
    private IUserGrain CurrentUser => _grainFactory.GetGrain<IUserGrain>(CurrentUserId);
    private CancellationToken ApplicationStopping => _applicationLifetime.ApplicationStopping;

    public ClientController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        IHostApplicationLifetime applicationLifetime)
    {
        _grainFactory = grainFactory;
        _jwtService = jwtTokenService;
        _applicationLifetime = applicationLifetime;
    }

    [HttpGet]
    [Authorize]
    [Route("/client/subscribe")]
    public async Task Subscribe()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var webSocketMessages = new List<string>();
            var webSocketTask = Task.Run(async () =>
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var cts = new CancellationTokenSource(millisecondsDelay: 1000);
                    var client = new ForwardedUserClient(_grainFactory, user: CurrentUser);
                    var clientWork = client.ObservationTaskForWebSocket(webSocketMessages, cts.Token);
                    await SendAsync(webSocket, webSocketMessages);
                    await clientWork;
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
            await webSocket.SendAsync(messageContent, messageType, true, default);
        }
        messages.Clear();
    }

    private async Task CloseAsync(WebSocket webSocket)
    {
        var status = WebSocketCloseStatus.EndpointUnavailable;
        await webSocket.CloseAsync(status, null, default);
        webSocket.Dispose();
    }
}
