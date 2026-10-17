using System.Net;
using InspectFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// Sign-in cannot be used to lock out someone else from one device, nor to learn which addresses have accounts
/// (see ADR 0009, update 2026-10-17). All test requests share one client IP, so the throttle key is the email.
/// </summary>
[Collection(ApiCollection.Name)]
public class SignInProtectionTests(TestApp app)
{
    private Task<ApiResponse> SignIn(string email, string password) =>
        app.CreateClient().PostAsync("/api/auth/login", new { email, password });

    [Fact]
    public async Task Five_failures_stop_this_device_without_locking_the_account()
    {
        var account = await app.CreateClient().RegisterAsync("Company");
        for (var i = 0; i < 5; i++) (await SignIn(account.Email, "WrongPass1")).EnsureStatus(HttpStatusCode.Unauthorized);

        var blocked = await SignIn(account.Email, TestData.Password);
        blocked.EnsureStatus(HttpStatusCode.TooManyRequests);
        Assert.Equal("too_many_attempts", blocked.Code);

        await using var db = app.CreateDbContext();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == account.UserId);
        Assert.Null(user.LockoutEnd); // the account owner can still sign in from their own device
        Assert.Equal(5, user.AccessFailedCount);
    }

    [Fact]
    public async Task A_locked_account_is_only_revealed_to_someone_who_knows_the_password()
    {
        var account = await app.CreateClient().RegisterAsync("Company");
        await using (var db = app.CreateDbContext())
            await db.Users.Where(u => u.Id == account.UserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.LockoutEnd, DateTimeOffset.UtcNow.AddMinutes(10)));

        var wrong = await SignIn(account.Email, "WrongPass1");
        wrong.EnsureStatus(HttpStatusCode.Unauthorized);
        Assert.Equal("Invalid email or password.", wrong["title"].GetValue<string>());

        var right = await SignIn(account.Email, TestData.Password);
        right.EnsureStatus(HttpStatusCode.Unauthorized);
        Assert.Contains("temporarily locked", right["title"].GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_get_the_same_answer()
    {
        var account = await app.CreateClient().RegisterAsync("Company");
        var unknown = await SignIn($"nobody-{Guid.NewGuid():N}@t.local", "WrongPass1");
        var wrong = await SignIn(account.Email, "WrongPass1");

        unknown.EnsureStatus(HttpStatusCode.Unauthorized);
        wrong.EnsureStatus(HttpStatusCode.Unauthorized);
        Assert.Equal(unknown["title"].GetValue<string>(), wrong["title"].GetValue<string>());
        Assert.Equal(unknown.Code, wrong.Code);
    }
}
