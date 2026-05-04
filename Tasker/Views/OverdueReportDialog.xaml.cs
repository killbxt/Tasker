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

            var ownedTeamIds = db.Teams.AsNoTracking()
                .Where(t => t.OwnerId == userId)
                .Select(t => t.Id)
                .ToList();

            if (ownedTeamIds.Count == 0)
            {
                HintText.Text =
                    "Вы не являетесь владельцем ни одной команды. Отчёт доступен только руководителю команды.";
                ReportGrid.ItemsSource = Array.Empty<OverdueTaskReportItem>();
                return;
            }

            HintText.Text =
                "Незавершённые задачи с истёкшим сроком по командам, где вы владелец. Указаны организация и команда (рабочая область).";

            var now = DateTime.Now;
            var tasks = db.Tasks.AsNoTracking()
                .Include(t => t.Team)!.ThenInclude(tm => tm!.Organization)
                .Include(t => t.AssignedTo)
                .Where(t => t.TeamId.HasValue && ownedTeamIds.Contains(t.TeamId.Value))
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
