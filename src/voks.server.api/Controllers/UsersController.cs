using Twilio;
using Twilio.Rest.Verify.V2.Service;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using voks.server.model;

namespace voks.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtTokenService;
    private readonly TwillioSettingsService _twillioSettings;

    public UsersController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        TwillioSettingsService twillioSettings)
    {
        _grainFactory = grainFactory;
        _jwtTokenService = jwtTokenService;
        _twillioSettings = twillioSettings;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/login/{phone}")]
    [Route("/login/{phone}/{code?}")]
    [Route("/register/{phone}")]
    [Route("/register/{phone}/{code?}")]
    [Route("/phone-verification/{phone}")]
    [Route("/phone-verification/{phone}/{code?}")]
    public async Task<IActionResult> VerificationProcess(string phone, string? code = null)
    {
        if (IsValidPhoneNumber(phone))
        {

            try
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
                        var usernameClaim = new Claim(ClaimTypes.Name, phone);
                        var phoneNumberClaim = new Claim(ClaimTypes.MobilePhone, phone);

                        var jwtToken = _jwtTokenService.CreateToken(new[] { usernameClaim, phoneNumberClaim });
                        var refreshToken = _jwtTokenService.CreateRefreshToken();
                        var refreshTokenExp = _jwtTokenService.CalculateRefreshTokenExpiry();

                        await userGrain.SetTokensAsync(new Tokens()
                        {
                            AccessToken = jwtToken.ToString(),
                            RefreshToken = refreshToken,
                            RefreshTokenExpires = refreshTokenExp
                        });

                        return Ok(new
                        {
                            Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                            RefreshToken = refreshToken,
                            Expiration = jwtToken.ValidTo
                        });
                    }
                    else if (check.Status == "pending") return Accepted();
                    else return Unauthorized();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            finally
            {
                ClearVerificationClient();
            }
        }
        return BadRequest();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("/refresh-tokens")]
    public async Task<IActionResult> RefreshToken([FromBody]TokenModel model)
    {
        if (model is null) return BadRequest("Invalid client request");
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null) return BadRequest("Invalid access token or refresh token");

        var user = _grainFactory.GetGrain<IUserGrain>(principal.Identity!.Name)!;
        if (user == null ||
            (await user.GetTokensAsync()).RefreshToken != model.RefreshToken ||
            (await user.GetTokensAsync()).RefreshTokenExpires <= DateTime.UtcNow)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        var newAccessToken = _jwtTokenService.CreateToken(principal.Claims.ToArray());
        var newRefreshToken = _jwtTokenService.CreateRefreshToken();

        await user.SetTokensAsync(new Tokens()
        {
            AccessToken = newAccessToken.ToString(),
            RefreshToken = newRefreshToken,
            RefreshTokenExpires = _jwtTokenService.CalculateRefreshTokenExpiry()
        });

        return Ok(new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
            RefreshToken = newRefreshToken,
            Expiration = newAccessToken.ValidTo
        });
    }

    [Authorize]
    [HttpDelete]
    [Route("/revoke-tokens")]
    public async Task<IActionResult> RevokeToken(string phone)
    {
        var user = _grainFactory.GetGrain<IUserGrain>(phone);
        if (user == null) return BadRequest("Invalid user phone number");

        await user.SetTokensAsync(new Tokens());

        return NoContent();
    }

    #region helpers
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
    #endregion

    #region models
    public class TokenModel
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
    #endregion
}
