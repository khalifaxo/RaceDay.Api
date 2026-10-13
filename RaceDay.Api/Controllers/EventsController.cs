using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] ValidTypes = { "Running", "Walking", "Cycling" };
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List all events. Optional filters: province, eventType, date.</summary>
    /// <response code="200">List of events.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(string? province, string? eventType, DateTime? date)
    {
        var query = _db.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(e => e.Province == province);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);
        if (date.HasValue)
            query = query.Where(e => e.EventDate.Date == date.Value.Date);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Province,
                e.EventType,
                e.DistanceKm,
                e.OrganiserId
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get the details of one event.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events
            .Where(x => x.EventId == id)
            .Select(x => new
            {
                x.EventId,
                x.EventName,
                x.Description,
                x.EventDate,
                x.Location,
                x.Province,
                x.EventType,
                x.DistanceKm,
                x.OrganiserId
            })
            .FirstOrDefaultAsync();

        return e == null ? NotFound(new { message = "Event not found." }) : Ok(e);
    }

    /// <summary>Create a new event. Organisers only.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        if (!ValidTypes.Contains(dto.EventType))
            return BadRequest(new { message = "EventType must be Running, Walking or Cycling." });

        var ev = new Event
        {
            OrganiserId = HttpContext.Session.GetInt32("UserId")!.Value,
            EventName = dto.EventName.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            Province = dto.Province.Trim(),
            EventType = dto.EventType,
            DistanceKm = dto.DistanceKm
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            ev.EventId,
            ev.EventName,
            ev.Description,
            ev.EventDate,
            ev.Location,
            ev.Province,
            ev.EventType,
            ev.DistanceKm,
            ev.OrganiserId
        });
    }

    /// <summary>Update an event. Only the Organiser who owns it can do this.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        if (!ValidTypes.Contains(dto.EventType))
            return BadRequest(new { message = "EventType must be Running, Walking or Cycling." });

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetInt32("UserId"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit your own events." });

        ev.EventName = dto.EventName.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location.Trim();
        ev.Province = dto.Province.Trim();
        ev.EventType = dto.EventType;
        ev.DistanceKm = dto.DistanceKm;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ev.EventId,
            ev.EventName,
            ev.Description,
            ev.EventDate,
            ev.Location,
            ev.Province,
            ev.EventType,
            ev.DistanceKm,
            ev.OrganiserId
        });
    }

    /// <summary>Delete an event. Only the Organiser who owns it can do this.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event already has enrolments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetInt32("UserId"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete your own events." });

        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category!.EventId == id);
        if (hasEnrolments)
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}