using Microsoft.EntityFrameworkCore;
using Tasker.Tests.Infrastructure;
using TaskManager.Data;
using TaskManager.Services;

namespace Tasker.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public void Register_AddsUser_WhenEmailIsUnique()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new ApplicationDbContext(options);
        var auth = new AuthService(db);

        var ok = auth.Register("alice", "alice@test.local", "secret123");

        Assert.True(ok);
        Assert.Single(db.Users);
        Assert.Equal("alice@test.local", db.Users.First().Email);
    }

    [Fact]
    public void Register_ReturnsFalse_WhenEmailAlreadyExists()
    {
        using var ws = TestWorkspace.Create();
        var auth = new AuthService(ws.Db);

        var ok = auth.Register("dup", ws.Owner.Email, "pwd");

        Assert.False(ok);
        Assert.Equal(2, ws.Db.Users.Count());
    }

    [Fact]
    public void Login_SetsCurrentUser_WhenCredentialsMatch()
    {
        using var ws = TestWorkspace.Create();
        var auth = new AuthService(ws.Db);

        var ok = auth.Login(ws.Owner.Email, "any");

        Assert.False(ok);

        auth = new AuthService(ws.Db);
        var registered = auth.Register("bob", "bob@test.local", "pass");
        Assert.True(registered);
        ok = auth.Login("bob@test.local", "pass");

        Assert.True(ok);
        Assert.NotNull(auth.CurrentUser);
        Assert.Equal("bob", auth.CurrentUser!.Username);
    }

    [Fact]
    public void Logout_ClearsCurrentUser()
    {
        using var ws = TestWorkspace.Create();
        var auth = new AuthService(ws.Db);
        auth.Register("bob", "bob@test.local", "pass");
        auth.Login("bob@test.local", "pass");

        auth.Logout();

        Assert.Null(auth.CurrentUser);
    }
}
