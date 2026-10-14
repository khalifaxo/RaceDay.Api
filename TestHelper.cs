using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public static class TestHelper
{
    /// <summary>A fresh, empty in-memory database for each test.</summary>
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Gives a controller a fake session. Pass no userId to simulate "not logged in".</summary>
    public static T WithSession<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var session = new TestSession();
        if (userId != null) session.SetInt32("UserId", userId.Value);
        if (role != null) session.SetString("Role", role);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }

    public static int StatusOf(IActionResult result) => result switch
    {
        ObjectResult o => o.StatusCode ?? 200,
        StatusCodeResult s => s.StatusCode,
        _ => 0
    };

    public static User AddUser(AppDbContext db, string role, string email)
    {
        var user = new User
        {
            FullName = $"Test {role}",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
            Role = role
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(AppDbContext db, int organiserId, string province = "Western Cape")
    {
        var ev = new Event
        {
            OrganiserId = organiserId,
            EventName = "Seed Event",
            EventDate = DateTime.Now.AddDays(30),
            Location = "Cape Town",
            Province = province,
            EventType = "Running",
            DistanceKm = 10
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(AppDbContext db, int eventId, int max = 100)
    {
        var cat = new Category
        {
            EventId = eventId,
            CategoryName = "10km",
            DistanceKm = 10,
            MaxParticipants = max,
            EntryFee = 150
        };
        db.Categories.Add(cat);
        db.SaveChanges();
        return cat;
    }

    public static Enrolment AddEnrolment(AppDbContext db, int participantId, int categoryId)
    {
        var en = new Enrolment { ParticipantId = participantId, CategoryId = categoryId };
        db.Enrolments.Add(en);
        db.SaveChanges();
        return en;
    }
}
