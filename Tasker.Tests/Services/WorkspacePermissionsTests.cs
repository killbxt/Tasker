using Tasker.Tests.Infrastructure;
using TaskManager.Services;

namespace Tasker.Tests.Services;

public class WorkspacePermissionsTests
{
    [Fact]
    public void IsOrganizationOwner_ReturnsTrue_ForOwner()
    {
        using var ws = TestWorkspace.Create();
        Assert.True(WorkspacePermissions.IsOrganizationOwner(ws.Db, ws.Owner.Id));
    }

    [Fact]
    public void IsOrganizationOwner_ReturnsFalse_ForParticipant()
    {
        using var ws = TestWorkspace.Create();
        Assert.False(WorkspacePermissions.IsOrganizationOwner(ws.Db, ws.Participant.Id));
    }

    [Fact]
    public void IsOrganizationParticipantOnly_ReturnsTrue_ForMemberNotOwner()
    {
        using var ws = TestWorkspace.Create();
        Assert.True(WorkspacePermissions.IsOrganizationParticipantOnly(ws.Db, ws.Participant.Id));
        Assert.False(WorkspacePermissions.IsOrganizationParticipantOnly(ws.Db, ws.Owner.Id));
    }

    [Fact]
    public void CanModifyTeamBoard_ReturnsTrue_ForOrgOwnerAndTeamOwner()
    {
        using var ws = TestWorkspace.Create();
        Assert.True(WorkspacePermissions.CanModifyTeamBoard(ws.Db, ws.Owner.Id, ws.Team));
    }

    [Fact]
    public void CanModifyTeamBoard_ReturnsFalse_ForParticipant()
    {
        using var ws = TestWorkspace.Create();
        Assert.False(WorkspacePermissions.CanModifyTeamBoard(ws.Db, ws.Participant.Id, ws.Team));
    }

    [Fact]
    public void CanUsePowerFeatures_ReturnsTrue_WhenUserHasNoOrganization()
    {
        using var ws = TestWorkspace.Create();
        var solo = new TaskManager.Models.User
        {
            Username = "solo",
            Email = "solo@test.local",
            PasswordHash = "x"
        };
        ws.Db.Users.Add(solo);
        ws.Db.SaveChanges();

        Assert.True(WorkspacePermissions.CanUsePowerFeatures(ws.Db, solo.Id));
    }

    [Fact]
    public void CanUsePowerFeatures_ReturnsFalse_ForOrgParticipant()
    {
        using var ws = TestWorkspace.Create();
        Assert.False(WorkspacePermissions.CanUsePowerFeatures(ws.Db, ws.Participant.Id));
        Assert.True(WorkspacePermissions.CanUsePowerFeatures(ws.Db, ws.Owner.Id));
    }

    [Fact]
    public void CanModifyPersonalTasks_ReturnsTrue_ForAnyPositiveUserId()
    {
        Assert.True(WorkspacePermissions.CanModifyPersonalTasks(1));
        Assert.False(WorkspacePermissions.CanModifyPersonalTasks(0));
    }
}
