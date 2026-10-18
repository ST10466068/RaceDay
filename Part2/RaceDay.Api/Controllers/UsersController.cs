using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/profile")]
public class UsersController(RaceDayContext db) : ControllerBase
{
    /// <summary>Returns the logged-in user's profile (any role).</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Get()
    {
        var u = await db.Users.Include(x => x.Role).FirstAsync(x => x.UserId == User.UserId());
        return Ok(new { u.FirstName, u.LastName, u.Email, role = u.Role.RoleName });
    }

    /// <summary>Updates the logged-in user's profile.</summary>
    /// <remarks>Body: firstName, lastName, email. Returns 200 with the updated profile, 400 if invalid, 409 if the email is taken.</remarks>
    [HttpPut]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(ProfileDto dto)
    {
        var id = User.UserId();
        var email = dto.Email.Trim().ToLower();
        if (await db.Users.AnyAsync(x => x.Email == email && x.UserId != id))
            return Conflict(new { error = "Email already in use." });

        var u = await db.Users.Include(x => x.Role).FirstAsync(x => x.UserId == id);
        u.FirstName = dto.FirstName.Trim();
        u.LastName = dto.LastName.Trim();
        u.Email = email;
        await db.SaveChangesAsync();
        return Ok(new { u.FirstName, u.LastName, u.Email, role = u.Role.RoleName });
    }
}