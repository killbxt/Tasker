using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services
{
    /// <summary>Права: владелец организации vs участник (приглашённый).</summary>
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

        /// <summary>Пользователь в организации, но не её владелец (приглашённый участник).</summary>
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

        /// <summary>Личные задачи (без команды): любой вошедший пользователь.</summary>
        public static bool CanModifyPersonalTasks(int userId) => userId > 0;

        /// <summary>Командная доска: полное создание/редактирование/удаление и очистка колонок — владелец организации или владелец команды.</summary>
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

        /// <summary>Устаревшее имя: раньше «всё подряд». Используйте <see cref="CanModifyPersonalTasks"/> или <see cref="CanModifyTeamBoard"/>.</summary>
        public static bool CanCreateAndEditTasks(ApplicationDbContext db, int userId)
        {
            return CanModifyPersonalTasks(userId);
        }

        /// <summary>AI, аналитика, отчёт просрочек, полное управление организацией. Соло-пользователь без организации — да.</summary>
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
