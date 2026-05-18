using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;
using Task = TaskManager.Models.Task;

namespace Tasker.Tests.Infrastructure;

/// <summary>
/// In-memory database with a small org: owner, participant, one team, sample tasks.
/// </summary>
public sealed class TestWorkspace : IDisposable
{
    public ApplicationDbContext Db { get; }
    public User Owner { get; }
    public User Participant { get; }
    public Organization Organization { get; }
    public Team Team { get; }
    public Task PersonalDoneTask { get; }
    public Task TeamDoneTask { get; }
    public Task TeamOverdueTask { get; }

    private TestWorkspace(
        ApplicationDbContext db,
        User owner,
        User participant,
        Organization organization,
        Team team,
        Task personalDone,
        Task teamDone,
        Task teamOverdue)
    {
        Db = db;
        Owner = owner;
        Participant = participant;
        Organization = organization;
        Team = team;
        PersonalDoneTask = personalDone;
        TeamDoneTask = teamDone;
        TeamOverdueTask = teamOverdue;
    }

    public static TestWorkspace Create(string? databaseName = null)
    {
        var name = databaseName ?? Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var db = new ApplicationDbContext(options);

        var owner = new User
        {
            Username = "owner",
            Email = "owner@test.local",
            PasswordHash = "hash"
        };
        var participant = new User
        {
            Username = "member",
            Email = "member@test.local",
            PasswordHash = "hash"
        };
        db.Users.AddRange(owner, participant);
        db.SaveChanges();

        var org = new Organization
        {
            Name = "Test Org",
            Description = "Demo",
            OwnerId = owner.Id,
            CreatedAt = DateTime.Now
        };
        db.Organizations.Add(org);
        db.SaveChanges();

        owner.OrganizationId = org.Id;
        participant.OrganizationId = org.Id;
        db.SaveChanges();

        var team = new Team
        {
            Name = "Alpha",
            Description = "Team A",
            OrganizationId = org.Id,
            OwnerId = owner.Id
        };
        db.Teams.Add(team);
        db.SaveChanges();

        owner.Teams.Add(team);
        participant.Teams.Add(team);
        db.SaveChanges();

        var now = DateTime.Now;
        var personalDone = new Task
        {
            Title = "Personal done",
            Status = TaskState.Done,
            Priority = TaskPriority.Normal,
            PlannedEndAt = now.AddDays(1),
            CreatedAt = now.AddDays(-10),
            CompletedAt = now.AddDays(-2),
            CreatedById = owner.Id,
            AssignedToId = owner.Id,
            TeamId = null
        };
        var teamDone = new Task
        {
            Title = "Team done",
            Status = TaskState.Done,
            Priority = TaskPriority.Urgent,
            PlannedEndAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-5),
            CompletedAt = now.AddDays(-1),
            CreatedById = owner.Id,
            AssignedToId = participant.Id,
            TeamId = team.Id
        };
        var teamOverdue = new Task
        {
            Title = "Team overdue",
            Status = TaskState.InProgress,
            Priority = TaskPriority.Normal,
            PlannedEndAt = now.AddDays(-3),
            CreatedAt = now.AddDays(-7),
            CreatedById = owner.Id,
            AssignedToId = participant.Id,
            TeamId = team.Id
        };
        db.Tasks.AddRange(personalDone, teamDone, teamOverdue);
        db.SaveChanges();

        return new TestWorkspace(db, owner, participant, org, team, personalDone, teamDone, teamOverdue);
    }

    public void Dispose() => Db.Dispose();
}
