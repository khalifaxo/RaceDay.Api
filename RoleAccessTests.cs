using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Controllers;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Tests;

public class RoleAccessTests
{
    private static AuthorizationFilterContext Context(TestSession session)
    {
        var http = new DefaultHttpContext { Session = session };
        return new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
    }

    private static TestSession LoggedIn(string role)
    {
        var s = new TestSession();
        s.SetInt32("UserId", 1);
        s.SetString("Role", role);
        return s;
    }

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var ctx = Context(new TestSession());

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(ctx);

        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public void Participant_OnOrganiserOnlyAction_IsRejectedWith403()
    {
        var ctx = Context(LoggedIn("Participant"));

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(ctx);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void Organiser_OnParticipantOnlyAction_IsRejectedWith403()
    {
        var ctx = Context(LoggedIn("Organiser"));

        new SessionAuthorizeAttribute("Participant").OnAuthorization(ctx);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void CorrectRole_IsAllowedThrough()
    {
        var ctx = Context(LoggedIn("Organiser"));

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(ctx);

        Assert.Null(ctx.Result); // no result set = request continues
    }

    [Fact]
    public void AnyLoggedInUser_PassesWhenNoRoleRequired()
    {
        var ctx = Context(LoggedIn("Participant"));

        new SessionAuthorizeAttribute().OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }

    // These check the filter is actually attached to the real endpoints.
    [Theory]
    [InlineData(typeof(EventsController), "Create", "Organiser")]
    [InlineData(typeof(EventsController), "Update", "Organiser")]
    [InlineData(typeof(EventsController), "Delete", "Organiser")]
    [InlineData(typeof(CategoriesController), "Create", "Organiser")]
    [InlineData(typeof(ResultsController), "Create", "Organiser")]
    [InlineData(typeof(EnrolmentsController), "Enrol", "Participant")]
    [InlineData(typeof(EnrolmentsController), "GetMine", "Participant")]
    [InlineData(typeof(ResultsController), "GetMine", "Participant")]
    public void Endpoint_IsProtectedWithTheCorrectRole(Type controller, string method, string expectedRole)
    {
        var action = controller.GetMethod(method)!;
        var attr = action.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attr);
        var role = typeof(SessionAuthorizeAttribute)
            .GetField("_role", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(attr);
        Assert.Equal(expectedRole, role);
    }
}
