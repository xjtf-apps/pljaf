using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Authorization;

using voks.server.model;
using voks.client.model;

namespace voks.server.api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MediaSettingsService _mediaSettingsService;
    private readonly FileExtensionContentTypeProvider _mimeMappingService;

    public ProfileController(
        IGrainFactory grainFactory,
        JwtTokenService jwtTokenService,
        MediaSettingsService mediaSettingsService)
    {
        _grainFactory = grainFactory;
        _jwtTokenService = jwtTokenService;
        _mediaSettingsService = mediaSettingsService;
        _mimeMappingService = new FileExtensionContentTypeProvider();
    }

    [HttpGet]
    [Authorize]
    [Route("/user/profile")]
    public async Task<IActionResult> GetUserProfile(string phoneNumber)
    {
        var contact = _grainFactory.GetGrain<IUserGrain>(phoneNumber);
        if (contact == null) return NoContent();

        var contactAvatar = await contact.GetAvatarAsync();
        var contactProfile = await contact.GetProfileAsync();

        return new JsonResult(new User()
        {
            Id = new UserId(phoneNumber),
            AvatarRef = contactAvatar != null ? new ImageRef() { StoreId = contactAvatar.StoreId } : null,
            DisplayName = contactProfile.DisplayName,
            StatusLine = contactProfile.StatusLine
        });
    }

    [HttpGet]
    //[Authorize] // WARN: dangerous!
    [AllowAnonymous]
    [Route("/user/profile/picture")]
    public async Task<IActionResult> GetUserProfilePicture(string phoneNumber)
    {
        var contact = _grainFactory.GetGrain<IUserGrain>(phoneNumber);
        var contactProfile = await contact.GetProfileAsync();
        if (contactProfile?.ProfilePicture != null &&
            contactProfile?.ProfilePicture.Filename != null)
        {
            var profilePicture = contactProfile.ProfilePicture;
            var filename = profilePicture.Filename;
            var data = profilePicture.BinaryData;
            var ext = Path.GetExtension(filename);

            return File(data, _mimeMappingService.Mappings[ext]);
        }
        return NoContent();
    }

    [HttpPost]
    [Authorize]
    [Route("/user/profile")]
    public async Task<IActionResult> SetUserProfile([FromBody]User model)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext);
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId);
        var currentProfile = await currentUser.GetProfileAsync();
        await currentUser.SetProfileAsync(new Profile()
        {
            DisplayName = model.DisplayName ?? currentProfile.DisplayName,
            StatusLine = model.StatusLine ?? currentProfile.StatusLine,
            ProfilePicture = currentProfile.ProfilePicture,
        });
        return Ok();
    }

    [HttpPost]
    [Authorize]
    [Route("/user/profile/picture")]
    public async Task<IActionResult> SetUserProfilePicture(IFormFile file)
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext);
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId);
        var currentProfile = await currentUser.GetProfileAsync();

        var maxSize = _mediaSettingsService.MaxProfilePictureSize;
        if (file.Length > maxSize) return BadRequest("File size too large");

        using var readStream = file.OpenReadStream();
        using var outputStream = new MemoryStream();
        await readStream.CopyToAsync(outputStream);

        await currentUser.SetProfileAsync(new Profile()
        {
            DisplayName = currentProfile.DisplayName,
            StatusLine = currentProfile.StatusLine,
            ProfilePicture = new Media()
            {
                 StoreId = Guid.NewGuid(),
                 Filename = file.FileName,
                 BinaryData = outputStream.ToArray()
            }
        });
        return Ok();
    }

    [Authorize]
    [HttpDelete]
    [Route("/user/profile/picture")]
    public async Task<IActionResult> ClearUserProfilePicture()
    {
        var currentUserId = _jwtTokenService.GetUserIdFromRequest(HttpContext);
        var currentUser = _grainFactory.GetGrain<IUserGrain>(currentUserId);
        var currentProfile = await currentUser.GetProfileAsync();

        await currentUser.SetProfileAsync(new Profile()
        {
            DisplayName = currentProfile.DisplayName,
            StatusLine = currentProfile.StatusLine,
            ProfilePicture = null
        });
        return Ok();
    }
}
