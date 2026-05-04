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

            if (!org.Members.Any(m => m.Id == userId))
            {
                error = "Вы не состоите в этой организации.";
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

            if (!org.Members.Any(m => m.Id == userId))
            {
                error = "Вы не состоите в этой организации.";
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

            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.OrganizationId != orgId)
            {
                error = "Сначала вступите в эту организацию.";
                return false;
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

            if (team.OwnerId != userId)
            {
                error = "Редактировать может только владелец команды.";
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

            if (team.OwnerId != userId)
            {
                error = "Удалить может только владелец команды.";
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
    }
}
