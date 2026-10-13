using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters;

/// <summary>
/// Blocks a request unless the user is logged in (session exists).
/// If a role is given, the logged-in user must also have that role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        // Not logged in -> 401
        if (session.GetInt32("UserId") == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must log in first." });
            return;
        }

        // Logged in but wrong role -> 403
        if (_role != null && session.GetString("Role") != _role)
        {
            context.Result = new ObjectResult(
                new { message = $"This action requires the {_role} role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}