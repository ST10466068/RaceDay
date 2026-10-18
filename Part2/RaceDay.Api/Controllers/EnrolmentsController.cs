using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Enrols the logged-in Participant into a category.</summary>
    /// <remarks>No body. Returns 201 when enrolled, 404 if the category does not exist, 409 if already enrolled.</remarks>
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat == null) return NotFound();
        var uid = User.UserId();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == categoryId && e.ParticipantId == uid))
            return Conflict(new { error = "Already enrolled in this category." });

        var en = new Enrolment { CategoryId = categoryId, ParticipantId = uid };
        db.Enrolments.Add(en);
        await db.SaveChangesAsync();
        return StatusCode(201, new { en.EnrolmentId, en.CategoryId, eventId = cat.EventId, en.Status, en.EnrolmentDate });
    }

    /// <summary>Lists everything the logged-in Participant has entered.</summary>
    [HttpGet("users/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        return Ok(await db.Enrolments.Where(e => e.ParticipantId == uid).Select(e => new
        {
            e.EnrolmentId,
            e.Status,
            e.EnrolmentDate,
            eventId = e.Category.EventId,
            eventName = e.Category.Event.Name,
            categoryId = e.CategoryId,
            categoryName = e.Category.Name
        }).ToListAsync());
    }

    /// <summary>Lets an Organiser see everyone enrolled in their event.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.UserId()) return Forbid();
        return Ok(await db.Enrolments.Where(e => e.Category.EventId == eventId).Select(e => new
        {
            e.EnrolmentId,
            e.Status,
            e.EnrolmentDate,
            participantId = e.ParticipantId,
            participantName = e.Participant.FirstName + " " + e.Participant.LastName,
            categoryId = e.CategoryId,
            categoryName = e.Category.Name
        }).ToListAsync());
    }

    /// <summary>Lets a Participant cancel their own enrolment.</summary>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id)
    {
        var en = await db.Enrolments.FindAsync(id);
        if (en == null) return NotFound();
        if (en.ParticipantId != User.UserId()) return Forbid();
        db.Enrolments.Remove(en);
        await db.SaveChangesAsync();
        return NoContent();
    }
}