using Microsoft.EntityFrameworkCore;
using Tasker.Tests.Infrastructure;
using TaskManager.Services;

namespace Tasker.Tests.Services;

public class WorkspaceAdminServiceTests
{
    [Fact]
    public void TryUpdateOrganization_Succeeds_ForOwner()
    {
        using var ws = TestWorkspace.Create();
        var ok = WorkspaceAdminService.TryUpdateOrganization(
            ws.Db, ws.Owner.Id, ws.Organization.Id, "  New Name  ", "Desc", out var error);

        Assert.True(ok);
        Assert.Empty(error);
        Assert.Equal("New Name", ws.Db.Organizations.Find(ws.Organization.Id)!.Name);
    }

    [Fact]
    public void TryUpdateOrganization_Fails_ForParticipant()
    {
        using var ws = TestWorkspace.Create();
        var ok = WorkspaceAdminService.TryUpdateOrganization(
            ws.Db, ws.Participant.Id, ws.Organization.Id, "Hack", null, out var error);

        Assert.False(ok);
        Assert.Contains("владелец", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateTeam_Succeeds_ForOrgOwner()
    {
        using var ws = TestWorkspace.Create();
        var ok = WorkspaceAdminService.TryCreateTeam(
            ws.Db, ws.Owner.Id, ws.Organization.Id, "Beta", "Second team", out var team, out var error);

        Assert.True(ok);
        Assert.NotNull(team);
        Assert.Empty(error);
        Assert.Equal(2, ws.Db.Teams.Count(t => t.OrganizationId == ws.Organization.Id));
    }

    [Fact]
    public void TryCreateTeam_Fails_ForParticipant()
    {
        using var ws = TestWorkspace.Create();
        var ok = WorkspaceAdminService.TryCreateTeam(
            ws.Db, ws.Participant.Id, ws.Organization.Id, "Beta", null, out _, out var error);

        Assert.False(ok);
        Assert.Contains("владелец", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryDeleteOrganization_ClearsMemberOrganizationAndTeamTasks()
    {
        using var ws = TestWorkspace.Create();
        var teamTaskId = ws.TeamDoneTask.Id;

        var ok = WorkspaceAdminService.TryDeleteOrganization(
            ws.Db, ws.Owner.Id, ws.Organization.Id, out var error);

        Assert.True(ok);
        Assert.Empty(error);
        Assert.False(ws.Db.Organizations.Any());
        Assert.Null(ws.Db.Users.Find(ws.Participant.Id)!.OrganizationId);
        Assert.Null(ws.Db.Tasks.Find(teamTaskId)!.TeamId);
    }

    [Fact]
    public void TryLeaveOrganizationAsMember_Fails_ForOwner()
    {
        using var ws = TestWorkspace.Create();
        var ok = WorkspaceAdminService.TryLeaveOrganizationAsMember(
            ws.Db, ws.Owner.Id, ws.Organization.Id, out var error);

        Assert.False(ok);
        Assert.Contains("Владелец", error);
    }

    [Fact]
    public void TryLeaveTeam_TransfersTeamOwnership_ToOrgOwner_WhenTeamOwnerLeaves()
    {
        using var ws = TestWorkspace.Create();
        ws.Team.OwnerId = ws.Participant.Id;
        ws.Db.SaveChanges();

        var ok = WorkspaceAdminService.TryLeaveTeam(ws.Db, ws.Participant.Id, ws.Team.Id, out var error);

        Assert.True(ok);
        Assert.Empty(error);
        var team = ws.Db.Teams.Include(t => t.Members).First(t => t.Id == ws.Team.Id);
        Assert.Equal(ws.Owner.Id, team.OwnerId);
        Assert.DoesNotContain(team.Members, m => m.Id == ws.Participant.Id);
    }
}
