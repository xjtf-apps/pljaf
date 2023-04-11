using pljaf.server.model;
using System.Diagnostics;

namespace pljaf.server.api;

public class ForwardedUserClient
{
    private List<string>? _messages;
    private readonly IUserGrain _userGrain;
    private readonly IGrainFactory _grainFactory;

    public ForwardedUserClient(IGrainFactory grainFactory, IUserGrain user)
    {
        _userGrain = user;
        _grainFactory = grainFactory;
    }

    public async Task ObservationTaskForWebSocket(List<string> messageOutbox, CancellationToken cancellation)
    {
        var observers_count = 0;
        var watch = Stopwatch.StartNew();
        var observers = new List<ConversationObserver>();
        var conversations = new List<IConversationGrain>();

        async Task Setup()
        {
            observers = new();
            _messages = messageOutbox;
            conversations = await _userGrain.GetConversationsAsync()!;

            for (int i = 0; i < conversations.Count; i++)
            {
                var conversation = conversations[i];
                var observer = new ConversationObserver(_grainFactory, conversation);
                observer.OnChange += ConversationObserver_OnChange;
                await observer.SubscribeToGrain();
                observers.Add(observer);
            }
            observers_count = observers.Count;
        }
        
        async Task<bool> Check()
        {
            var num_conversations = await _userGrain.GetConversationsCountAsync()!;
            var check = num_conversations != observers_count;
            return check;
        }

        async Task Drop()
        {
            for (int i = 0; i < observers!.Count; i++)
            {
                var observer = observers[i];
                await observer.UnsubscribeFromGrain();
                observer.OnChange -= ConversationObserver_OnChange;

                await observer.DisposeAsync();
            }
            observers.Clear();
            observers_count = 0;
            conversations!.Clear();
        }
            
        async Task Loop()
        {
            await Setup();
            while (true)
            {
                var elapsed = watch.Elapsed.TotalMilliseconds;
                if (elapsed % 20 == 0)
                {
                    if (await Check())
                    {
                        await Drop();
                        await Setup();
                        watch.Restart();
                    }
                }
                await Task.Delay(200, cancellation);
            }
        }

        await Task.Run(Loop, cancellation).ContinueWith(async (_) => await Drop());
    }

    private void ConversationObserver_OnChange(object? sender, string e)
    {
        _messages?.Add(e);
    }
}