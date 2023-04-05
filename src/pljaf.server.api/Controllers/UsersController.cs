using Microsoft.AspNetCore.Mvc;
using pljaf.server.model;

using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace pljaf.server.api.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private IGrainFactory _grainFactory;
    private TwillioUserVerificationSettings _twillioSettings;

    public UsersController(IGrainFactory grainFactory, TwillioUserVerificationSettings twillioSettings)
    {
        _grainFactory = grainFactory;
        _twillioSettings = twillioSettings;
    }

    public static readonly Dictionary<string, Guid> _registeredUsers = new();
    public static readonly Dictionary<string, Guid> _registeringUsers = new();

    [HttpGet]
    [Route("/register/{phone}")]
    public async Task<IActionResult> StartRegistrationProcess(string phone)
    {
        if (IsValidPhoneNumber(phone))
        {
            if (_registeredUsers.ContainsKey(phone))
            {
                // redirect to 2FA for new devices of already registered users
                throw new NotImplementedException();
            }
            else
            {
                var userId = Guid.NewGuid();
                _registeringUsers[phone] = userId;

                TwilioClient.Init(_twillioSettings.AccountSid, _twillioSettings.AccountSid);

                var verification = await VerificationResource.CreateAsync
                    (to: phone, channel: "sms", pathServiceSid: _twillioSettings.ServiceSid);

                return new JsonResult(verification.Status);
            }
        }
        else return BadRequest();
    }

    private bool IsValidPhoneNumber(string phone)
    {
        if (phone == null) return false;
        if (phone.StartsWith("+") &&
            phone.Skip(1).All(c => char.IsDigit(c))) return true;

        return false;
    }

    //[HttpGet]
    //[Route("/{phone}")]
    //public async Task<IActionResult> GetUserAsync(string phone)
    //{
    //    if (_registeredUsers.TryGetValue(phone, out var userId))
    //    {
    //        var userGrain = _grainFactory.GetGrain<IUserGrain>(userId)!;
    //        var userProfile = await userGrain.GetProfileAsync();
    //        return new JsonResult(userProfile);
    //    }
    //    return NoContent();
    //}
}
