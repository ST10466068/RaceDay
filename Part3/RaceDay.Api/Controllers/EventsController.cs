using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Lists all events (public).</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Events.OrderBy(e => e.EventDate)
            .Select(e => new { e.EventId, e.Name, e.Description, e.EventDate, e.Location, e.EventType })
            .ToListAsync());

    /// <summary>Returns one event with its categories (public).</summary>
    [HttpGet("{id:int}", Name = "GetEvent")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOne(int id)
    {
        var e = await db.Events.Where(x => x.EventId == id).Select(x => new
        {
            x.EventId,
            x.Name,
            x.Description,
            x.EventDate,
            x.Location,
            x.EventType,
            categories = x.Categories.Select(c => new { c.CategoryId, c.Name, c.DistanceKm, c.Fee })
        }).FirstOrDefaultAsync();
        return e == null ? NotFound() : Ok(e);
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    /// <remarks>Body: name, description, eventDate, location, eventType (Run, Walk or Cycle).</remarks>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var e = new Event
        {
            OrganiserId = User.UserId(),
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            EventType = dto.EventType
        };
        db.Events.Add(e);
        await db.SaveChangesAsync();
        return CreatedAtRoute("GetEvent", new { id = e.EventId },
            new { e.EventId, e.Name, e.Description, e.EventDate, e.Location, e.EventType });
    }

    /// <summary>Updates an event the Organiser owns.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var e = await db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != User.UserId()) return Forbid();
        e.Name = dto.Name; e.Description = dto.Description; e.EventDate = dto.EventDate;
        e.Location = dto.Location; e.EventType = dto.EventType;
        await db.SaveChangesAsync();
        return Ok(new { e.EventId, e.Name, e.Description, e.EventDate, e.Location, e.EventType });
    }

    /// <summary>Deletes an event the Organiser owns (also removes its categories, enrolments and results).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != User.UserId()) return Forbid();
        db.Events.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}