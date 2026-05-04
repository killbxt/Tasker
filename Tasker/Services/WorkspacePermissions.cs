using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services
{
    public static class WorkspacePermissions
    {
        public static bool IsOrganizationOwner(ApplicationDbContext db, int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            var orgId = db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (orgId == null)
            {
                return false;
            }

            return db.Organizations.AsNoTracking()
                .Any(o => o.Id == orgId.Value && o.OwnerId == userId);
        }

        public static bool IsOrganizationParticipantOnly(ApplicationDbContext db, int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            var orgId = db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (orgId == null)
            {
                return false;
            }

            return !db.Organizations.AsNoTracking()
                .Any(o => o.Id == orgId.Value && o.OwnerId == userId);
        }

        public static bool CanModifyPersonalTasks(int userId) => userId > 0;

        public static bool CanModifyTeamBoard(ApplicationDbContext db, int userId, Team? team)
        {
            if (userId <= 0 || team == null)
            {
                return false;
            }

            var isOrgOwner = db.Organizations.AsNoTracking()
                .Any(o => o.Id == team.OrganizationId && o.OwnerId == userId);
            return isOrgOwner || team.OwnerId == userId;
        }

        public static bool CanCreateAndEditTasks(ApplicationDbContext db, int userId)
        {
            return CanModifyPersonalTasks(userId);
        }

        public static bool CanUsePowerFeatures(ApplicationDbContext db, int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            var orgId = db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (orgId == null)
            {
                return true;
            }

            return db.Organizations.AsNoTracking()
                .Any(o => o.Id == orgId.Value && o.OwnerId == userId);
        }
    }
}
