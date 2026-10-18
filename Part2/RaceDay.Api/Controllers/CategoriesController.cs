using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController(RaceDayContext db) : ControllerBase
{
    /// <summary>Lists the categories of an event (public).</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        return Ok(await db.Categories.Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.Name, c.DistanceKm, c.Fee }).ToListAsync());
    }

    /// <summary>Adds a category to an event the Organiser owns.</summary>
    /// <remarks>Body: name, distanceKm, fee.</remarks>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.UserId()) return Forbid();
        var c = new Category { EventId = eventId, Name = dto.Name, DistanceKm = dto.DistanceKm, Fee = dto.Fee };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return StatusCode(201, new { c.CategoryId, c.Name, c.DistanceKm, c.Fee });
    }

    /// <summary>Edits a category the Organiser owns.</summary>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryId == id);
        if (c == null) return NotFound();
        if (c.Event.OrganiserId != User.UserId()) return Forbid();
        c.Name = dto.Name; c.DistanceKm = dto.DistanceKm; c.Fee = dto.Fee;
        await db.SaveChangesAsync();
        return Ok(new { c.CategoryId, c.Name, c.DistanceKm, c.Fee });
    }

    /// <summary>Removes a category the Organiser owns.</summary>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryId == id);
        if (c == null) return NotFound();
        if (c.Event.OrganiserId != User.UserId()) return Forbid();
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
