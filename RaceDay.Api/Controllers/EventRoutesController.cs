using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/routes")]
public class EventRoutesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventRoutesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>View the route information for an event.</summary>
    /// <response code="200">List of routes.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var routes = await _db.EventRoutes
            .Where(r => r.EventId == eventId)
            .Select(r => new
            {
                r.RouteId,
                r.EventId,
                r.RouteName,
                r.DistanceKm,
                r.ElevationGainM,
                r.StartPoint,
                r.EndPoint,
                r.GpxFileUrl
            })
            .ToListAsync();

        return Ok(routes);
    }

    /// <summary>Add route information to an event. Only the Organiser who owns the event.</summary>
    /// <response code="201">Route created.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, RouteDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetInt32("UserId"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add routes to your own events." });

        var route = new EventRoute
        {
            EventId = eventId,
            RouteName = dto.RouteName.Trim(),
            DistanceKm = dto.DistanceKm,
            ElevationGainM = dto.ElevationGainM,
            StartPoint = dto.StartPoint.Trim(),
            EndPoint = dto.EndPoint.Trim(),
            GpxFileUrl = dto.GpxFileUrl
        };

        _db.EventRoutes.Add(route);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            route.RouteId,
            route.EventId,
            route.RouteName,
            route.DistanceKm,
            route.ElevationGainM,
            route.StartPoint,
            route.EndPoint,
            route.GpxFileUrl
        });
    }
}