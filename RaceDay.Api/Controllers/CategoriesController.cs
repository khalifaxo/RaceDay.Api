using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>List the categories of an event.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new
            {
                c.CategoryId,
                c.EventId,
                c.CategoryName,
                c.DistanceKm,
                c.MaxParticipants,
                c.EntryFee
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Add a category to an event. Only the Organiser who owns the event.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{eventId}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            category.CategoryId,
            category.EventId,
            category.CategoryName,
            category.DistanceKm,
            category.MaxParticipants,
            category.EntryFee
        });
    }

    /// <summary>Update a category. Only the Organiser who owns the event.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit categories on your own events." });

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKm = dto.DistanceKm;
        category.MaxParticipants = dto.MaxParticipants;
        category.EntryFee = dto.EntryFee;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            category.CategoryId,
            category.EventId,
            category.CategoryName,
            category.DistanceKm,
            category.MaxParticipants,
            category.EntryFee
        });
    }

    /// <summary>Delete a category. Only the Organiser who owns the event.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category already has enrolments.</response>
    [HttpDelete("categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories on your own events." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { message = "This category already has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}