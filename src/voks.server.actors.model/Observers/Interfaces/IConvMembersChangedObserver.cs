namespace voks.server.model;

public interface IConvMembersChangedObserver : IGrainObserver
{
    Task OnMemberJoined(IUserGrain newMember);
    Task OnMemberLeft(IUserGrain leftMember);
}
