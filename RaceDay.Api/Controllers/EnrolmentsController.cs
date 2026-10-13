using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>Enter an event by choosing a category. Participants only.</summary>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">The category is full.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">You are already enrolled in this category.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        var taken = await _db.Enrolments
            .CountAsync(e => e.CategoryId == category.CategoryId && e.Status == "Confirmed");
        if (taken >= category.MaxParticipants)
            return BadRequest(new { message = "This category is full." });

        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.ParticipantId == CurrentUserId && e.CategoryId == dto.CategoryId);

        if (enrolment != null)
        {
            if (enrolment.Status == "Confirmed")
                return Conflict(new { message = "You are already enrolled in this category." });

            // Previously cancelled: re-activate it (one row per participant per category)
            enrolment.Status = "Confirmed";
            enrolment.EnrolmentDate = DateTime.Now;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantId = CurrentUserId,
                CategoryId = dto.CategoryId
            };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();

        // Bib number built from the ids so it is unique
        enrolment.BibNumber = $"E{category.EventId}-{enrolment.EnrolmentId:D4}";
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.CategoryId,
            EventId = category.EventId,
            EventName = category.Event!.EventName,
            CategoryName = category.CategoryName,
            enrolment.BibNumber,
            enrolment.Status,
            enrolment.EnrolmentDate
        });
    }

    /// <summary>View your own enrolments. Participants only.</summary>
    /// <response code="200">Your enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/my")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var list = await _db.Enrolments
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.CategoryId,
                CategoryName = e.Category!.CategoryName,
                EventId = e.Category.EventId,
                EventName = e.Category.Event!.EventName,
                EventDate = e.Category.Event.EventDate,
                e.BibNumber,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>View all enrolments for an event. Only the Organiser who owns the event.</summary>
    /// <response code="200">Enrolments for the event.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });

        var list = await _db.Enrolments
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                ParticipantName = e.Participant!.FullName,
                ParticipantEmail = e.Participant.Email,
                e.CategoryId,
                CategoryName = e.Category!.CategoryName,
                e.BibNumber,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Cancel your own enrolment. Participants only.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("enrolments/{id}")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only cancel your own enrolments." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { message = "A result already exists for this enrolment." });

        // Marked Cancelled rather than deleted, matching the Status column in the database
        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return NoContent();
    }
}