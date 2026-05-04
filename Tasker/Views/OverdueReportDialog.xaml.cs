using Microsoft.EntityFrameworkCore;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class OverdueReportDialog : Window
    {
        public OverdueReportDialog(AuthService authService)
        {
            InitializeComponent();
            LoadReport(authService);
        }

        private void LoadReport(AuthService authService)
        {
            if (authService.CurrentUser == null)
            {
                Close();
                return;
            }

            var userId = authService.CurrentUser.Id;
            using var db = new ApplicationDbContext();

            var orgId = db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();

            if (orgId == null || !db.Organizations.AsNoTracking().Any(o => o.Id == orgId && o.OwnerId == userId))
            {
                HintText.Text =
                    "Отчёт доступен только владельцу организации (просрочки по всем командам организации).";
                ReportGrid.ItemsSource = Array.Empty<OverdueTaskReportItem>();
                return;
            }

            var teamIds = db.Teams.AsNoTracking()
                .Where(t => t.OrganizationId == orgId)
                .Select(t => t.Id)
                .ToList();

            if (teamIds.Count == 0)
            {
                HintText.Text = "В организации пока нет команд.";
                ReportGrid.ItemsSource = Array.Empty<OverdueTaskReportItem>();
                return;
            }

            HintText.Text =
                "Незавершённые задачи с истёкшим сроком по всем командам вашей организации.";

            var now = DateTime.Now;
            var tasks = db.Tasks.AsNoTracking()
                .Include(t => t.Team)!.ThenInclude(tm => tm!.Organization)
                .Include(t => t.AssignedTo)
                .Where(t => t.TeamId.HasValue && teamIds.Contains(t.TeamId.Value))
                .Where(t => t.Status != TaskState.Done && t.PlannedEndAt < now)
                .OrderBy(t => t.PlannedEndAt)
                .ToList();

            var rows = tasks.Select(t => new OverdueTaskReportItem
            {
                OrganizationName = t.Team!.Organization.Name,
                TeamName = t.Team.Name,
                TaskTitle = t.Title,
                AssigneeName = t.AssignedTo != null ? t.AssignedTo.Username : "—",
                PlannedEndAt = t.PlannedEndAt
            }).ToList();

            ReportGrid.ItemsSource = rows;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
