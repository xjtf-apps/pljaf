using pljaf.server.model;

namespace pljaf.server.api;

public class ForwardedClientObserverService : IForwardedClientObserver
{
    public event EventHandler<string>? OnChangeObserved;

    public Task DownloadAttachedMedia(Media? mediaRef)
    {
        throw new NotImplementedException();
    }

    public Task OnMemberInvited(IUserGrain inviter, IUserGrain invited)
    {
        throw new NotImplementedException();
    }

    public Task OnMemberJoined(IUserGrain newMember)
    {
        throw new NotImplementedException();
    }

    public Task OnMemberLeft(IUserGrain leftMember)
    {
        throw new NotImplementedException();
    }

    public Task OnMessagePosted(IMessageGrain message)
    {
        throw new NotImplementedException();
    }

    public Task OnNameChanged(string name)
    {
        throw new NotImplementedException();
    }

    public Task OnTopicChanged(string topic)
    {
        throw new NotImplementedException();
    }

    public Task ReceiveSentConfirmation(DateTime timestamp)
    {
        throw new NotImplementedException();
    }
}
