using Tasker.Tests.Infrastructure;
using TaskManager.Models;
using TaskManager.Services;

namespace Tasker.Tests.Services;

public class WorkspaceAnalyticsServiceTests
{
    [Fact]
    public void GetMyWorkspaceStats_CountsPersonalAndTeamMetrics()
    {
        using var ws = TestWorkspace.Create();
        var rows = WorkspaceAnalyticsService.GetMyWorkspaceStats(ws.Db, ws.Owner.Id);

        var personal = rows.First(r => r.TeamId == null);
        Assert.Equal(1, personal.CompletedTotal);
        Assert.Equal(0, personal.OpenOverdueNow);

        var teamRow = rows.First(r => r.TeamId == ws.Team.Id);
        Assert.Equal(0, teamRow.CompletedTotal);
    }

    [Fact]
    public void GetMyWorkspaceStats_ParticipantSeesTeamDoneAndOverdue()
    {
        using var ws = TestWorkspace.Create();
        var rows = WorkspaceAnalyticsService.GetMyWorkspaceStats(ws.Db, ws.Participant.Id);

        var teamRow = rows.First(r => r.TeamId == ws.Team.Id);
        Assert.Equal(1, teamRow.CompletedTotal);
        Assert.Equal(1, teamRow.OpenOverdueNow);
        Assert.Equal(1, teamRow.UrgentDoneTotal);
    }

    [Fact]
    public void GetTeamMemberPeriodStats_CountsCompletedAndLateInPeriod()
    {
        using var ws = TestWorkspace.Create();
        var from = DateTime.Today.AddDays(-30);
        var to = DateTime.Today;

        var stats = WorkspaceAnalyticsService.GetTeamMemberPeriodStats(
            ws.Db, ws.Owner.Id, ws.Team.Id, from, to);

        var memberRow = stats.First(r => r.UserId == ws.Participant.Id);
        Assert.Equal(1, memberRow.CompletedInPeriod);
        Assert.Equal(1, memberRow.OpenOverdueAtPeriodEnd);
        Assert.Equal(0, memberRow.LateCompletedInPeriod);
    }

    [Fact]
    public void GetTeamMemberPeriodStats_CountsLateCompletion_WhenDoneAfterPlannedEnd()
    {
        using var ws = TestWorkspace.Create();
        var now = DateTime.Now;
        ws.TeamDoneTask.PlannedEndAt = now.AddDays(-5);
        ws.TeamDoneTask.CompletedAt = now.AddDays(-1);
        ws.Db.SaveChanges();

        var stats = WorkspaceAnalyticsService.GetTeamMemberPeriodStats(
            ws.Db, ws.Owner.Id, ws.Team.Id, now.AddDays(-10), now);

        var memberRow = stats.First(r => r.UserId == ws.Participant.Id);
        Assert.Equal(1, memberRow.LateCompletedInPeriod);
        Assert.Equal(2, memberRow.TotalDueBreaches);
    }

    [Fact]
    public void CanOwnerAccessTeam_ReturnsFalse_ForWrongOrganization()
    {
        using var ws = TestWorkspace.Create();
        var outsider = new User
        {
            Username = "other",
            Email = "other@test.local",
            PasswordHash = "h"
        };
        ws.Db.Users.Add(outsider);
        ws.Db.SaveChanges();

        Assert.False(WorkspaceAnalyticsService.CanOwnerAccessTeam(ws.Db, outsider.Id, ws.Team.Id));
        Assert.True(WorkspaceAnalyticsService.CanOwnerAccessTeam(ws.Db, ws.Owner.Id, ws.Team.Id));
    }

    [Fact]
    public void GetLegacyBoardDoneStats_ParticipantSeesOnlyOwnDoneTasks()
    {
        using var ws = TestWorkspace.Create();
        var (total, _, _, top) = WorkspaceAnalyticsService.GetLegacyBoardDoneStats(
            ws.Db, ws.Participant.Id, ws.Team);

        Assert.Equal(1, total);
        Assert.Single(top);
        Assert.Equal("member", top[0].Name);
    }

    [Fact]
    public void GetOrganizationTeamsForOwner_ReturnsAllOrgTeams()
    {
        using var ws = TestWorkspace.Create();
        var teams = WorkspaceAnalyticsService.GetOrganizationTeamsForOwner(ws.Db, ws.Owner.Id);

        Assert.Single(teams);
        Assert.Equal("Alpha", teams[0].Name);
    }
}
