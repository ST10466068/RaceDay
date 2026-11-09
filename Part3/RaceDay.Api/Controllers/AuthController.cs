using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(RaceDayContext db, IConfiguration cfg) : ControllerBase
{
    /// <summary>Registers a new Organiser or Participant.</summary>
    /// <remarks>Body: firstName, lastName, email, password (min 8), role ("Organiser" or "Participant"). Returns 201 with user id and role, 400 if invalid, 409 if email is taken.</remarks>
    [HttpPost("register")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { error = "Role must be Organiser or Participant." });

        var email = dto.Email.Trim().ToLower();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "Email already in use." });

        var role = await db.Roles.FirstAsync(r => r.RoleName == dto.Role);
        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return StatusCode(201, new { userId = user.UserId, role = role.RoleName });
    }

    /// <summary>Logs in and returns a JWT.</summary>
    /// <remarks>Body: email, password. Returns 200 with token, user id and role, or 401 for invalid credentials.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials." });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(
            cfg["Jwt:Issuer"], cfg["Jwt:Audience"],
            new[] { new Claim("uid", user.UserId.ToString()), new Claim("role", user.Role.RoleName) },
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt), userId = user.UserId, role = user.Role.RoleName });
    }
}