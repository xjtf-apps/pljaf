namespace voks.server.model;

public interface IConvInvitesChangedObserver : IGrainObserver
{
    Task OnMemberInvited(IUserGrain inviter, IUserGrain invited);
}
