/using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UploadsController(RaceDayContext db, BlobService blob) : ControllerBase
{
    static readonly string[] Ext = { ".jpg", ".jpeg", ".png", ".webp" };
    static bool Valid(IFormFile? f) =>
        f != null && f.Length > 0 && f.Length <= 5 * 1024 * 1024 &&
        Ext.Contains(Path.GetExtension(f.FileName).ToLower());

    /// <summary>Uploads a banner image for an event the Organiser owns (stored in Azure Blob Storage).</summary>
    /// <remarks>Multipart form with a "file" field. JPG, PNG or WEBP up to 5MB. Returns 200 with the image URL.</remarks>
    [HttpPost("events/{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Banner(int id, IFormFile file)
    {
        var e = await db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != User.UserId()) return Forbid();
        if (!Valid(file)) return BadRequest(new { error = "Upload a JPG, PNG or WEBP image under 5MB." });
        e.BannerUrl = await blob.UploadAsync(file, "banners");
        await db.SaveChangesAsync();
        return Ok(new { url = e.BannerUrl });
    }

    /// <summary>Uploads the logged-in user's profile picture (stored in Azure Blob Storage).</summary>
    /// <remarks>Multipart form with a "file" field. JPG, PNG or WEBP up to 5MB. Returns 200 with the image URL.</remarks>
    [HttpPost("users/profile/picture")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Picture(IFormFile file)
    {
        if (!Valid(file)) return BadRequest(new { error = "Upload a JPG, PNG or WEBP image under 5MB." });
        var u = await db.Users.FindAsync(User.UserId());
        u!.ProfilePictureUrl = await blob.UploadAsync(file, "profiles");
        await db.SaveChangesAsync();
        return Ok(new { url = u.ProfilePictureUrl });
    }
}