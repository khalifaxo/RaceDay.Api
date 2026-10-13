using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Finished", "DNF", "DSQ" };
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>Capture a result for a participant's enrolment. Only the Organiser who owns the event.</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Invalid status, time or enrolment.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">This enrolment already has a result.</response>
    [HttpPost("results")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateResultDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Status must be Finished, DNF or DSQ." });

        TimeSpan? finish = null;
        if (!string.IsNullOrWhiteSpace(dto.FinishTime))
        {
            if (!TimeSpan.TryParse(dto.FinishTime, out var parsed))
                return BadRequest(new { message = "FinishTime must look like 03:45:10." });
            finish = parsed;
        }
        if (dto.Status == "Finished" && finish == null)
            return BadRequest(new { message = "A finished result needs a FinishTime." });

        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for your own events." });

        if (enrolment.Status != "Confirmed")
            return BadRequest(new { message = "That enrolment was cancelled." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId))
            return Conflict(new { message = "This enrolment already has a result. Use PUT to change it." });

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            FinishTime = finish,
            Position = dto.Position,
            Status = dto.Status,
            CapturedByUserId = CurrentUserId
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.Position,
            result.Status,
            result.CapturedAt
        });
    }

    /// <summary>Correct a captured result. Only the Organiser who owns the event.</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid status or time.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateResultDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Status must be Finished, DNF or DSQ." });

        TimeSpan? finish = null;
        if (!string.IsNullOrWhiteSpace(dto.FinishTime))
        {
            if (!TimeSpan.TryParse(dto.FinishTime, out var parsed))
                return BadRequest(new { message = "FinishTime must look like 03:45:10." });
            finish = parsed;
        }
        if (dto.Status == "Finished" && finish == null)
            return BadRequest(new { message = "A finished result needs a FinishTime." });

        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound(new { message = "Result not found." });

        if (result.Enrolment!.Category!.Event!.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change results for your own events." });

        result.FinishTime = finish;
        result.Position = dto.Position;
        result.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.Position,
            result.Status,
            result.CapturedAt
        });
    }

    /// <summary>View your own result history. Participants only.</summary>
    /// <response code="200">Your results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("results/my")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var list = await _db.Results
            .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.CapturedAt)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                EventName = r.Enrolment!.Category!.Event!.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                CategoryName = r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.Position,
                r.Status
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>View all results for an event. Public.</summary>
    /// <response code="200">Results for the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var list = await _db.Results
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Position ?? int.MaxValue)
            .Select(r => new
            {
                r.ResultId,
                ParticipantName = r.Enrolment!.Participant!.FullName,
                CategoryName = r.Enrolment.Category!.CategoryName,
                r.FinishTime,
                r.Position,
                r.Status
            })
            .ToListAsync();

        return Ok(list);
    }
}
