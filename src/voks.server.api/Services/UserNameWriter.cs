using voks.server.model;

namespace voks.server.api;

public class UserNameWriter
{
    public async Task<string> GetFirstNameOrId(IUserGrain user)
    {
        return (await user.GetProfileAsync()).DisplayName?.Split(' ')[0] ??
               (await user.GetIdAsync());
    }

    public async Task<string> GetDisplayNameOrId(IUserGrain user)
    {
        return (await user.GetProfileAsync()).DisplayName ??
               (await user.GetIdAsync());
    }
}
