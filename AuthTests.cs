using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class AuthTests
{
    private static RegisterDto Reg(string role = "Participant", string email = "new@test.com") => new()
    {
        FullName = "New User",
        Email = email,
        Password = "Pass1234",
        Role = role
    };

    [Fact]
    public async Task Register_ValidUser_Returns201_AndStoresHashedPassword()
    {
        using var db = TestHelper.NewDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Register(Reg());

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Users);
        Assert.NotEqual("Pass1234", saved.PasswordHash);                     // not stored in plain text
        Assert.True(BCrypt.Net.BCrypt.Verify("Pass1234", saved.PasswordHash)); // but verifies correctly
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        using var db = TestHelper.NewDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Register(Reg(role: "Admin"));

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddUser(db, "Participant", "new@test.com");
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Register(Reg(email: "NEW@test.com")); // email check ignores case

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200_AndStartsSession()
    {
        using var db = TestHelper.NewDb();
        var user = TestHelper.AddUser(db, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "org@test.com", Password = "Pass1234" });

        Assert.Equal(200, TestHelper.StatusOf(result));
        var session = controller.HttpContext.Session;
        Assert.Equal(user.UserId, session.GetInt32("UserId"));
        Assert.Equal("Organiser", session.GetString("Role"));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_AndNoSession()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddUser(db, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "org@test.com", Password = "WrongPass" });

        Assert.Equal(401, TestHelper.StatusOf(result));
        Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = TestHelper.NewDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "nobody@test.com", Password = "Pass1234" });

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        using var db = TestHelper.NewDb();
        var controller = TestHelper.WithSession(new AuthController(db), userId: 1, role: "Organiser");

        controller.Logout();

        Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
    }
}
