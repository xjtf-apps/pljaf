using Twilio;
using pljaf.server.model;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    [Route("/phone-verification/{phone}")]
    [Route("/phone-verification/{phone}/{code?}")]
    public async Task<IActionResult> VerificationProcess(string phone, string? code = null)
    {
        if (IsValidPhoneNumber(phone))
        {
            InitVerificationClient();

            if (code == null)
            {
                var verification = await VerificationResource.CreateAsync
                    (to: phone, channel: "sms", pathServiceSid: _twillioSettings.ServiceSid);

                return new JsonResult(verification.Status);
            }
            else
            {
                var check = await VerificationCheckResource.CreateAsync
                    (to: phone, code: code.ToString(), pathServiceSid: _twillioSettings.ServiceSid);

                if (check.Status == "approved")
                {
                    var userGrain = _grainFactory.GetGrain<IUserGrain>(phone)!;
                    var userProfile = await userGrain.GetProfileAsync()!;

                    // NOTE: should use client model here!
                    return new JsonResult(userProfile);
                }
            }
            ClearVerificationClient();
        }
        return BadRequest();
    }

    private void InitVerificationClient()
    {
        TwilioClient.Init(_twillioSettings.AccountSid, _twillioSettings.AuthToken);
    }

    private static void ClearVerificationClient()
    {
        TwilioClient.Invalidate();
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        if (phone == null) return false;
        if (phone.StartsWith("+") &&
            phone.Skip(1).All(char.IsDigit)) return true;

        return false;
    }
}
