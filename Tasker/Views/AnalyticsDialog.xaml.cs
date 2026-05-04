using Microsoft.EntityFrameworkCore;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class AnalyticsDialog : Window
    {
        private readonly AuthService _authService;
        private readonly Team? _selectedTeam;
        private readonly ApplicationDbContext _context;

        public AnalyticsDialog(AuthService authService, Team? selectedTeam)
        {
            InitializeComponent();
            _authService = authService;
            _selectedTeam = selectedTeam;
            _context = new ApplicationDbContext();

            LoadAnalytics();
        }

        private void LoadAnalytics()
        {
            var user = _authService.CurrentUser;
            if (user == null)
            {
                Close();
                return;
            }

            var userId = user.Id;

            IQueryable<Models.Task> query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Where(t => t.Status == TaskState.Done);

            if (_selectedTeam != null)
            {
                var team = _context.Teams.AsNoTracking().FirstOrDefault(t => t.Id == _selectedTeam.Id);
                if (team == null)
                {
                    ScopeTitle.Text = "Аналитика";
                    return;
                }

                ScopeTitle.Text = $"Аналитика: {team.Name}";

                if (team.OwnerId == userId)
                {
                    query = query.Where(t => t.TeamId == team.Id);
                }
                else
                {
                    query = query.Where(t => t.TeamId == team.Id && t.AssignedToId == userId);
                }
            }
            else
            {
                ScopeTitle.Text = "Аналитика: личные задачи";
                query = query.Where(t => t.TeamId == null && (t.AssignedToId == userId || t.CreatedById == userId));
            }

            var tasks = query.ToList();

            var totalDone = tasks.Count;
            TotalDoneText.Text = totalDone.ToString();

            var since = DateTime.Now.AddDays(-7);
            var done7 = tasks.Count(t => (t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt) >= since);
            Done7DaysText.Text = done7.ToString();

            var leadTimes = tasks
                .Where(t => t.CompletedAt != null)
                .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
                .Where(h => h >= 0 && h < 24 * 365)
                .ToList();

            if (leadTimes.Count == 0)
            {
                AvgLeadTimeText.Text = "—";
            }
            else
            {
                var avgHours = leadTimes.Average();
                AvgLeadTimeText.Text = avgHours >= 48
                    ? $"{avgHours / 24:0.#} дн"
                    : $"{avgHours:0.#} ч";
            }

            var top = tasks
                .Where(t => t.AssignedTo != null)
                .GroupBy(t => new { t.AssignedTo!.Id, t.AssignedTo!.Username })
                .Select(g => new { Name = g.Key.Username, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(8)
                .ToList();

            TopAssigneesList.ItemsSource = top;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

