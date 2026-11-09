using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Captures a finish time and position for an enrolment (Organiser who owns the event).</summary>
    /// <remarks>Body: finishTime (e.g. "01:45:30"), position. Returns 201, 404 if the enrolment is not found, 409 if a result already exists.</remarks>
    [HttpPost("enrolments/{id:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Capture(int id, ResultDto dto)
    {
        var en = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (en == null) return NotFound();
        if (en.Category.Event.OrganiserId != User.UserId()) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { error = "Result already captured." });

        var r = new Result { EnrolmentId = id, FinishTime = dto.FinishTime, Position = dto.Position };
        db.Results.Add(r);
        await db.SaveChangesAsync();
        return StatusCode(201, new { r.ResultId, r.EnrolmentId, r.FinishTime, r.Position });
    }

    /// <summary>Returns the logged-in Participant's results history.</summary>
    [HttpGet("users/results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        return Ok(await db.Results.Where(r => r.Enrolment.ParticipantId == uid).Select(r => new
        {
            r.ResultId,
            r.FinishTime,
            r.Position,
            eventName = r.Enrolment.Category.Event.Name,
            categoryName = r.Enrolment.Category.Name
        }).ToListAsync());
    }

    /// <summary>Public leaderboard for an event.</summary>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        return Ok(await db.Results.Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Position)
            .Select(r => new
            {
                r.Position,
                r.FinishTime,
                participant = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                categoryName = r.Enrolment.Category.Name
            }).ToListAsync());
    }
}