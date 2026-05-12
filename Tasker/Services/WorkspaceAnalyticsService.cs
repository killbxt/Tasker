using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services
{
    public static class WorkspaceAnalyticsService
    {
        public sealed class MyWorkspaceStatRow
        {
            public string WorkspaceName { get; init; } = "";
            public int? TeamId { get; init; }
            public int CompletedTotal { get; init; }
            public int CompletedLast7Days { get; init; }
            public int CompletedLast30Days { get; init; }
            public int InProgressNow { get; init; }
            public int OpenOverdueNow { get; init; }
            public int UrgentDoneTotal { get; init; }
        }

        public sealed class TeamMemberPeriodRow
        {
            public int UserId { get; init; }
            public string Username { get; init; } = "";
            public int CompletedInPeriod { get; init; }
            public int OpenOverdueAtPeriodEnd { get; init; }
            public int LateCompletedInPeriod { get; init; }
            public int TotalDueBreaches => OpenOverdueAtPeriodEnd + LateCompletedInPeriod;
        }

        private sealed class TaskLite
        {
            public int? TeamId { get; init; }
            public int? AssignedToId { get; init; }
            public int CreatedById { get; init; }
            public TaskState Status { get; init; }
            public DateTime? CompletedAt { get; init; }
            public DateTime? UpdatedAt { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime PlannedEndAt { get; init; }
            public TaskPriority Priority { get; init; }
        }

        public static IReadOnlyList<MyWorkspaceStatRow> GetMyWorkspaceStats(ApplicationDbContext db, int userId)
        {
            var teamIds = db.Teams.AsNoTracking()
                .Where(t => t.Members.Any(m => m.Id == userId))
                .Select(t => t.Id)
                .ToList();

            var tasks = db.Tasks.AsNoTracking()
                .Where(t =>
                    (t.TeamId == null && (t.AssignedToId == userId || t.CreatedById == userId)) ||
                    (t.TeamId.HasValue && teamIds.Contains(t.TeamId.Value) && t.AssignedToId == userId))
                .Select(t => new TaskLite
                {
                    TeamId = t.TeamId,
                    AssignedToId = t.AssignedToId,
                    CreatedById = t.CreatedById,
                    Status = t.Status,
                    CompletedAt = t.CompletedAt,
                    UpdatedAt = t.UpdatedAt,
                    CreatedAt = t.CreatedAt,
                    PlannedEndAt = t.PlannedEndAt,
                    Priority = t.Priority
                })
                .ToList();

            var rows = new List<MyWorkspaceStatRow> { BuildRow("Личные задачи", null, tasks, userId) };

            var teamNames = db.Teams.AsNoTracking()
                .Where(t => teamIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToList();

            foreach (var tm in teamNames)
            {
                rows.Add(BuildRow(tm.Name, tm.Id, tasks, userId));
            }

            return rows;
        }

        private static MyWorkspaceStatRow BuildRow(
            string name,
            int? teamId,
            List<TaskLite> tasks,
            int userId)
        {
            bool InScope(TaskLite t)
            {
                if (teamId == null)
                {
                    return t.TeamId == null && (t.AssignedToId == userId || t.CreatedById == userId);
                }

                return t.TeamId == teamId && t.AssignedToId == userId;
            }

            var scoped = tasks.Where(InScope).ToList();
            var done = scoped.Where(t => t.Status == TaskState.Done).ToList();
            var totalDone = done.Count;

            int CountDoneSince(DateTime since) =>
                done.Count(t =>
                {
                    var at = t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt;
                    return at >= since;
                });

            var inProgress = scoped.Count(t => t.Status == TaskState.InProgress);
            var openOverdue = scoped.Count(t =>
                t.Status != TaskState.Done && t.PlannedEndAt < DateTime.Now);

            var urgentDone = done.Count(t => t.Priority == TaskPriority.Urgent);

            return new MyWorkspaceStatRow
            {
                WorkspaceName = name,
                TeamId = teamId,
                CompletedTotal = totalDone,
                CompletedLast7Days = CountDoneSince(DateTime.Now.AddDays(-7)),
                CompletedLast30Days = CountDoneSince(DateTime.Now.AddDays(-30)),
                InProgressNow = inProgress,
                OpenOverdueNow = openOverdue,
                UrgentDoneTotal = urgentDone
            };
        }

        public static IReadOnlyList<Team> GetOrganizationTeamsForOwner(ApplicationDbContext db, int ownerUserId)
        {
            if (!WorkspacePermissions.IsOrganizationOwner(db, ownerUserId))
            {
                return Array.Empty<Team>();
            }

            var orgId = db.Users.AsNoTracking()
                .Where(u => u.Id == ownerUserId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (orgId == null)
            {
                return Array.Empty<Team>();
            }

            return db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .Where(t => t.OrganizationId == orgId.Value)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public static bool CanOwnerAccessTeam(ApplicationDbContext db, int ownerUserId, int teamId)
        {
            var team = db.Teams.AsNoTracking().FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                return false;
            }

            return db.Organizations.AsNoTracking()
                .Any(o => o.Id == team.OrganizationId && o.OwnerId == ownerUserId);
        }

        public static IReadOnlyList<TeamMemberPeriodRow> GetTeamMemberPeriodStats(
            ApplicationDbContext db,
            int ownerUserId,
            int teamId,
            DateTime from,
            DateTime to)
        {
            if (!CanOwnerAccessTeam(db, ownerUserId, teamId))
            {
                return Array.Empty<TeamMemberPeriodRow>();
            }

            var team = db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                return Array.Empty<TeamMemberPeriodRow>();
            }

            var start = from.Date;
            var end = to.Date.AddDays(1).AddTicks(-1);

            var assigneeIdsOnBoard = db.Tasks.AsNoTracking()
                .Where(t => t.TeamId == teamId && t.AssignedToId != null)
                .Select(t => t.AssignedToId!.Value)
                .Distinct()
                .ToList();

            var memberIds = team.Members.Select(m => m.Id)
                .Append(team.OwnerId)
                .Concat(assigneeIdsOnBoard)
                .Distinct()
                .ToList();
            if (memberIds.Count == 0)
            {
                return Array.Empty<TeamMemberPeriodRow>();
            }

            var names = db.Users.AsNoTracking()
                .Where(u => memberIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username })
                .ToDictionary(x => x.Id, x => x.Username);

            var tasks = db.Tasks.AsNoTracking()
                .Where(t => t.TeamId == teamId && t.AssignedToId != null && memberIds.Contains(t.AssignedToId.Value))
                .Select(t => new
                {
                    t.AssignedToId,
                    t.Status,
                    t.CompletedAt,
                    t.UpdatedAt,
                    t.CreatedAt,
                    t.PlannedEndAt
                })
                .ToList();

            var completedByUser = tasks
                .Where(t => t.Status == TaskState.Done)
                .Select(t => new
                {
                    Uid = t.AssignedToId!.Value,
                    DoneAt = t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt
                })
                .Where(x => x.DoneAt >= start && x.DoneAt <= end)
                .GroupBy(x => x.Uid)
                .ToDictionary(g => g.Key, g => g.Count());

            var lateByUser = tasks
                .Where(t => t.Status == TaskState.Done)
                .Select(t => new
                {
                    Uid = t.AssignedToId!.Value,
                    DoneAt = t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt,
                    t.PlannedEndAt
                })
                .Where(x => x.DoneAt >= start && x.DoneAt <= end && x.DoneAt > x.PlannedEndAt)
                .GroupBy(x => x.Uid)
                .ToDictionary(g => g.Key, g => g.Count());

            var openOverdueByUser = tasks
                .Where(t => t.Status != TaskState.Done && t.PlannedEndAt <= end)
                .GroupBy(t => t.AssignedToId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return memberIds
                .OrderBy(id => names.TryGetValue(id, out var n) ? n : id.ToString())
                .Select(id => new TeamMemberPeriodRow
                {
                    UserId = id,
                    Username = names.TryGetValue(id, out var un) ? un : $"#{id}",
                    CompletedInPeriod = completedByUser.TryGetValue(id, out var c) ? c : 0,
                    OpenOverdueAtPeriodEnd = openOverdueByUser.TryGetValue(id, out var o) ? o : 0,
                    LateCompletedInPeriod = lateByUser.TryGetValue(id, out var l) ? l : 0
                })
                .ToList();
        }

        public static (int TotalDone, int Done7, double? AvgLeadHours, IReadOnlyList<(string Name, int Count)> TopAssignees)
            GetLegacyBoardDoneStats(ApplicationDbContext db, int viewerUserId, Team? selectedTeam)
        {
            IQueryable<Models.Task> query = db.Tasks
                .AsNoTracking()
                .Include(t => t.AssignedTo)
                .Where(t => t.Status == TaskState.Done);

            if (selectedTeam != null)
            {
                var team = db.Teams.AsNoTracking().FirstOrDefault(t => t.Id == selectedTeam.Id);
                if (team == null)
                {
                    return (0, 0, null, Array.Empty<(string, int)>());
                }

                if (WorkspacePermissions.CanModifyTeamBoard(db, viewerUserId, team))
                {
                    query = query.Where(t => t.TeamId == team.Id);
                }
                else
                {
                    query = query.Where(t => t.TeamId == team.Id && t.AssignedToId == viewerUserId);
                }
            }
            else
            {
                query = query.Where(t =>
                    t.TeamId == null && (t.AssignedToId == viewerUserId || t.CreatedById == viewerUserId));
            }

            var tasks = query.ToList();
            var totalDone = tasks.Count;
            var since = DateTime.Now.AddDays(-7);
            var done7 = tasks.Count(t => (t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt) >= since);

            var leadTimes = tasks
                .Where(t => t.CompletedAt != null)
                .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
                .Where(h => h >= 0 && h < 24 * 365)
                .ToList();

            double? avg = leadTimes.Count == 0 ? null : leadTimes.Average();

            var top = tasks
                .Where(t => t.AssignedTo != null)
                .GroupBy(t => new { t.AssignedTo!.Id, t.AssignedTo!.Username })
                .Select(g => (g.Key.Username, g.Count()))
                .OrderByDescending(x => x.Item2)
                .ThenBy(x => x.Item1)
                .Take(8)
                .ToList();

            return (totalDone, done7, avg, top);
        }
    }
}
