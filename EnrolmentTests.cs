using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedCorrectly()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var ev = TestHelper.AddEvent(db, org.UserId);
        var cat = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), runner.UserId, "Participant");

        var result = await controller.Enrol(new EnrolDto { CategoryId = cat.CategoryId });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Enrolments);
        Assert.Equal(runner.UserId, saved.ParticipantId);  // linked to the participant
        Assert.Equal(cat.CategoryId, saved.CategoryId);    // linked to the chosen category
        Assert.Equal("Confirmed", saved.Status);
        Assert.False(string.IsNullOrEmpty(saved.BibNumber));
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        using var db = TestHelper.NewDb();
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var controller = TestHelper.WithSession(new EnrolmentsController(db), runner.UserId, "Participant");

        var result = await controller.Enrol(new EnrolDto { CategoryId = 999 });

        Assert.Equal(404, TestHelper.StatusOf(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), runner.UserId, "Participant");

        await controller.Enrol(new EnrolDto { CategoryId = cat.CategoryId });
        var second = await controller.Enrol(new EnrolDto { CategoryId = cat.CategoryId });

        Assert.Equal(409, TestHelper.StatusOf(second));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns400()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var first = TestHelper.AddUser(db, "Participant", "p1@test.com");
        var second = TestHelper.AddUser(db, "Participant", "p2@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId, max: 1);
        TestHelper.AddEnrolment(db, first.UserId, cat.CategoryId); // takes the only place
        var controller = TestHelper.WithSession(new EnrolmentsController(db), second.UserId, "Participant");

        var result = await controller.Enrol(new EnrolDto { CategoryId = cat.CategoryId });

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var me = TestHelper.AddUser(db, "Participant", "me@test.com");
        var someoneElse = TestHelper.AddUser(db, "Participant", "else@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        TestHelper.AddEnrolment(db, me.UserId, cat.CategoryId);
        TestHelper.AddEnrolment(db, someoneElse.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), me.UserId, "Participant");

        var result = await controller.GetMine();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Single((IEnumerable<object>)ok.Value!);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var ev = TestHelper.AddEvent(db, org.UserId);
        var cat = TestHelper.AddCategory(db, ev.EventId);
        TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), org.UserId, "Organiser");

        var result = await controller.GetForEvent(ev.EventId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Single((IEnumerable<object>)ok.Value!);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsForSomeoneElsesEvent()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddUser(db, "Organiser", "owner@test.com");
        var other = TestHelper.AddUser(db, "Organiser", "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.UserId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), other.UserId, "Organiser");

        var result = await controller.GetForEvent(ev.EventId);

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        var en = TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), runner.UserId, "Participant");

        var result = await controller.Cancel(en.EnrolmentId);

        Assert.Equal(204, TestHelper.StatusOf(result));
        Assert.Equal("Cancelled", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var owner = TestHelper.AddUser(db, "Participant", "owner@test.com");
        var intruder = TestHelper.AddUser(db, "Participant", "intruder@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        var en = TestHelper.AddEnrolment(db, owner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), intruder.UserId, "Participant");

        var result = await controller.Cancel(en.EnrolmentId);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Equal("Confirmed", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_ForOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        var en = TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new ResultsController(db), org.UserId, "Organiser");

        var result = await controller.Create(new CreateResultDto
        {
            EnrolmentId = en.EnrolmentId,
            FinishTime = "00:45:10",
            Position = 3,
            Status = "Finished"
        });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Results);
        Assert.Equal(3, saved.Position);
        Assert.Equal(new TimeSpan(0, 45, 10), saved.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        using var db = TestHelper.NewDb();
        var org = TestHelper.AddUser(db, "Organiser", "o@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, org.UserId).EventId);
        var en = TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new ResultsController(db), org.UserId, "Organiser");
        var dto = new CreateResultDto { EnrolmentId = en.EnrolmentId, FinishTime = "00:45:10", Position = 3, Status = "Finished" };

        await controller.Create(dto);
        var second = await controller.Create(dto);

        Assert.Equal(409, TestHelper.StatusOf(second));
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForSomeoneElsesEvent()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddUser(db, "Organiser", "owner@test.com");
        var other = TestHelper.AddUser(db, "Organiser", "other@test.com");
        var runner = TestHelper.AddUser(db, "Participant", "p@test.com");
        var cat = TestHelper.AddCategory(db, TestHelper.AddEvent(db, owner.UserId).EventId);
        var en = TestHelper.AddEnrolment(db, runner.UserId, cat.CategoryId);
        var controller = TestHelper.WithSession(new ResultsController(db), other.UserId, "Organiser");

        var result = await controller.Create(new CreateResultDto
        {
            EnrolmentId = en.EnrolmentId, FinishTime = "00:45:10", Position = 1, Status = "Finished"
        });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Results);
    }
}
