using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize] // any logged-in user, either role
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>View your own profile.</summary>
    /// <response code="200">Your profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Role, user.CreatedAt });
    }

    /// <summary>Update your own name and email.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Email already used by someone else.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var email = dto.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return BadRequest(new { message = "That email is already used by another account." });

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Role });
    }
}
