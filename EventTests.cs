using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class EventTests
{
    private static EventDto ValidDto(string type = "Running") => new()
    {
        EventName = "Test Run",
        Description = "A test event",
        EventDate = DateTime.Now.AddDays(30),
        Location = "Cape Town",
        Province = "Western Cape",
        EventType = type,
        DistanceKm = 10
    };

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var result = await controller.Create(ValidDto());

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Events);
        Assert.Equal(org.UserId, saved.OrganiserId);   // owner is the logged-in organiser
        Assert.Equal(10, saved.DistanceKm);
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var result = await controller.Create(ValidDto(type: "Swimming"));

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var ev = TestHelper.AddEvent(db, org.UserId);
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var dto = ValidDto();
        dto.EventName = "Renamed Event";
        var result = await controller.Update(ev.EventId, dto);

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal("Renamed Event", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddUser(db, "Organiser", "owner@test.com");
        var other = TestHelper.AddUser(db, "Organiser", "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.UserId);
        var controller = TestHelper.WithSession(new EventsController(db), other.UserId, "Organiser");

        var dto = ValidDto();
        dto.EventName = "Hijacked";
        var result = await controller.Update(ev.EventId, dto);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Equal("Seed Event", db.Events.Single().EventName); // unchanged
    }

    [Fact]
    public async Task Update_MissingEvent_Returns404()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var result = await controller.Update(999, ValidDto());

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var ev = TestHelper.AddEvent(db, org.UserId);
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(204, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddUser(db, "Organiser", "owner@test.com");
        var other = TestHelper.AddUser(db, "Organiser", "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.UserId);
        var controller = TestHelper.WithSession(new EventsController(db), other.UserId, "Organiser");

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_Returns409()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var ev = TestHelper.AddEvent(db, org.UserId);
        var cat = TestHelper.AddCategory(db, ev.EventId);
        TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new EventsController(db), org.UserId, "Organiser");

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task GetAll_FiltersByProvince()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        TestHelper.AddEvent(db, org.UserId, "Western Cape");
        TestHelper.AddEvent(db, org.UserId, "Gauteng");
        var controller = new EventsController(db); // public endpoint: no session needed

        var result = await controller.GetAll("Gauteng", null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Single((IEnumerable<object>)ok.Value!);
    }

    [Fact]
    public async Task GetById_UnknownEvent_Returns404()
    {
        using var db = TestHelper.NewDb();
        var controller = new EventsController(db);

        var result = await controller.GetById(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }
}
