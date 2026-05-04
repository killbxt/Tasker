using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services
{
    public static class WorkspaceAdminService
    {
        public static bool TryUpdateOrganization(
            ApplicationDbContext db, int userId, int orgId, string name, string? description, out string error)
        {
            error = "";
            var org = db.Organizations
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Id == orgId);

            if (org == null)
            {
                error = "Организация не найдена.";
                return false;
            }

            if (!db.Organizations.AsNoTracking().Any(o => o.Id == orgId && o.OwnerId == userId))
            {
                error = "Изменять организацию может только её владелец.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Название не может быть пустым.";
                return false;
            }

            org.Name = name.Trim();
            org.Description = description?.Trim() ?? "";
            db.SaveChanges();
            return true;
        }

        public static bool TryDeleteOrganization(ApplicationDbContext db, int userId, int orgId, out string error)
        {
            error = "";
            var org = db.Organizations
                .Include(o => o.Teams)
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Id == orgId);

            if (org == null)
            {
                error = "Организация не найдена.";
                return false;
            }

            if (!db.Organizations.AsNoTracking().Any(o => o.Id == orgId && o.OwnerId == userId))
            {
                error = "Удалять организацию может только её владелец.";
                return false;
            }

            var teamIds = org.Teams.Select(t => t.Id).ToList();
            if (teamIds.Count > 0)
            {
                var tasks = db.Tasks.Where(t => t.TeamId.HasValue && teamIds.Contains(t.TeamId.Value));
                foreach (var task in tasks)
                {
                    task.TeamId = null;
                }
            }

            foreach (var m in org.Members.ToList())
            {
                m.OrganizationId = null;
            }

            db.SaveChanges();
            db.Teams.RemoveRange(org.Teams);
            db.Organizations.Remove(org);
            db.SaveChanges();
            return true;
        }

        public static bool TryCreateTeam(
            ApplicationDbContext db,
            int userId,
            int orgId,
            string name,
            string? description,
            out Team? team,
            out string error)
        {
            team = null;
            error = "";

            var userOrgId = db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (userOrgId != orgId)
            {
                error = "Сначала вступите в эту организацию.";
                return false;
            }

            if (!db.Organizations.AsNoTracking().Any(o => o.Id == orgId && o.OwnerId == userId))
            {
                error = "Создавать команды может только владелец организации.";
                return false;
            }

            foreach (var entry in db.ChangeTracker.Entries<User>().Where(e => e.Entity.Id == userId).ToList())
            {
                entry.State = EntityState.Detached;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Нужно название команды.";
                return false;
            }

            team = new Team
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? "",
                OrganizationId = orgId,
                OwnerId = userId
            };

            db.Teams.Add(team);
            db.SaveChanges();

            var userTracked = db.Users.Include(u => u.Teams).First(u => u.Id == userId);
            userTracked.Teams.Add(team);
            db.SaveChanges();
            return true;
        }

        public static bool TryUpdateTeam(
            ApplicationDbContext db, int userId, int teamId, string name, string? description, out string error)
        {
            error = "";
            var team = db.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                error = "Команда не найдена.";
                return false;
            }

            if (!db.Organizations.AsNoTracking().Any(o => o.Id == team.OrganizationId && o.OwnerId == userId))
            {
                error = "Изменять команду может только владелец организации.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Название не может быть пустым.";
                return false;
            }

            team.Name = name.Trim();
            team.Description = description?.Trim() ?? "";
            db.SaveChanges();
            return true;
        }

        public static bool TryDeleteTeam(ApplicationDbContext db, int userId, int teamId, out string error)
        {
            error = "";
            var team = db.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                error = "Команда не найдена.";
                return false;
            }

            if (!db.Organizations.AsNoTracking().Any(o => o.Id == team.OrganizationId && o.OwnerId == userId))
            {
                error = "Удалять команду может только владелец организации.";
                return false;
            }

            var tasks = db.Tasks.Where(t => t.TeamId == teamId);
            foreach (var task in tasks)
            {
                task.TeamId = null;
            }

            db.SaveChanges();
            db.Teams.Remove(team);
            db.SaveChanges();
            return true;
        }

        /// <summary>Покинуть команду: капитаном становится владелец организации.</summary>
        public static bool TryLeaveTeam(ApplicationDbContext db, int userId, int teamId, out string error)
        {
            error = "";
            var team = db.Teams.Include(t => t.Members).FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                error = "Команда не найдена.";
                return false;
            }

            var member = team.Members.FirstOrDefault(m => m.Id == userId);
            if (member == null)
            {
                error = "Вы не состоите в этой команде.";
                return false;
            }

            if (team.OwnerId == userId)
            {
                var orgOwnerId = db.Organizations.AsNoTracking()
                    .Where(o => o.Id == team.OrganizationId)
                    .Select(o => o.OwnerId)
                    .FirstOrDefault();
                if (orgOwnerId == 0)
                {
                    error = "Не удалось определить владельца организации.";
                    return false;
                }

                team.OwnerId = orgOwnerId;
            }

            team.Members.Remove(member);
            db.SaveChanges();
            return true;
        }

        /// <summary>Покинуть организацию (только не владелец). Убирает из всех команд этой организации.</summary>
        public static bool TryLeaveOrganizationAsMember(ApplicationDbContext db, int userId, int orgId, out string error)
        {
            error = "";
            if (db.Organizations.AsNoTracking().Any(o => o.Id == orgId && o.OwnerId == userId))
            {
                error = "Владелец не может выйти из организации без её полного удаления.";
                return false;
            }

            var user = db.Users.Include(u => u.Teams).FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                error = "Пользователь не найден.";
                return false;
            }

            if (user.OrganizationId != orgId)
            {
                error = "Вы не состоите в этой организации.";
                return false;
            }

            var orgOwnerId = db.Organizations.AsNoTracking()
                .Where(o => o.Id == orgId)
                .Select(o => o.OwnerId)
                .FirstOrDefault();
            if (orgOwnerId == 0)
            {
                error = "Организация не найдена.";
                return false;
            }

            foreach (var t in user.Teams.Where(t => t.OrganizationId == orgId).ToList())
            {
                var teamEntity = db.Teams.Include(tm => tm.Members).First(tm => tm.Id == t.Id);
                var m = teamEntity.Members.FirstOrDefault(x => x.Id == userId);
                if (m != null)
                {
                    teamEntity.Members.Remove(m);
                }

                if (teamEntity.OwnerId == userId)
                {
                    teamEntity.OwnerId = orgOwnerId;
                }
            }

            user.OrganizationId = null;
            db.SaveChanges();
            return true;
        }
    }
}
