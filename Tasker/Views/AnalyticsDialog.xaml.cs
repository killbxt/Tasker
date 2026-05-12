using System.Windows;
using System.Windows.Controls;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class AnalyticsDialog : Window
    {
        private readonly AuthService _authService;
        private readonly Team? _selectedTeamFromBoard;
        private readonly ApplicationDbContext _context;

        public AnalyticsDialog(AuthService authService, Team? selectedTeamFromBoard)
        {
            InitializeComponent();
            _authService = authService;
            _selectedTeamFromBoard = selectedTeamFromBoard;
            _context = new ApplicationDbContext();
            Closed += (_, _) => _context.Dispose();

            if (_authService.CurrentUser == null)
            {
                Close();
                return;
            }

            var isOrgOwner = WorkspacePermissions.IsOrganizationOwner(_context, _authService.CurrentUser.Id);
            if (!isOrgOwner)
            {
                OwnerTab.Visibility = Visibility.Collapsed;
            }

            OwnerFromPicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            OwnerToPicker.SelectedDate = DateTime.Today;

            if (isOrgOwner)
            {
                var teams = WorkspaceAnalyticsService.GetOrganizationTeamsForOwner(_context, _authService.CurrentUser.Id)
                    .ToList();
                OwnerTeamCombo.ItemsSource = teams;
                if (teams.Count > 0)
                {
                    var pre = _selectedTeamFromBoard != null
                        ? teams.FirstOrDefault(t => t.Id == _selectedTeamFromBoard.Id)
                        : null;
                    OwnerTeamCombo.SelectedItem = pre ?? teams[0];
                }
            }

            LoadMyTab();
            if (isOrgOwner)
            {
                OwnerRefresh_Click(this, new RoutedEventArgs());
            }
        }

        private void LoadMyTab()
        {
            var user = _authService.CurrentUser;
            if (user == null)
            {
                return;
            }

            var rows = WorkspaceAnalyticsService.GetMyWorkspaceStats(_context, user.Id);
            MyStatsGrid.ItemsSource = rows;

            var team = _selectedTeamFromBoard;
            if (team != null)
            {
                BoardScopeHint.Text = $"Сводка по области на доске: {team.Name}";
            }
            else
            {
                BoardScopeHint.Text = "Сводка по области на доске: личные задачи";
            }

            var (total, done7, avgHours, top) =
                WorkspaceAnalyticsService.GetLegacyBoardDoneStats(_context, user.Id, team);
            TotalDoneText.Text = total.ToString();
            Done7DaysText.Text = done7.ToString();
            if (avgHours == null)
            {
                AvgLeadTimeText.Text = "—";
            }
            else
            {
                AvgLeadTimeText.Text = avgHours >= 48
                    ? $"{avgHours.Value / 24:0.#} дн"
                    : $"{avgHours.Value:0.#} ч";
            }

            TopAssigneesList.ItemsSource = top.Select(x => new { x.Name, Count = x.Count }).ToList();
        }

        private void OwnerRefresh_Click(object sender, RoutedEventArgs e)
        {
            var user = _authService.CurrentUser;
            if (user == null || OwnerTeamCombo.SelectedItem is not Team team)
            {
                OwnerPeriodGrid.ItemsSource = null;
                return;
            }

            var from = OwnerFromPicker.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to = OwnerToPicker.SelectedDate ?? DateTime.Today;
            if (to < from)
            {
                MessageBox.Show("Дата «по» не может быть раньше даты «с».", "Период", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var stats = WorkspaceAnalyticsService.GetTeamMemberPeriodStats(_context, user.Id, team.Id, from, to);
            OwnerPeriodGrid.ItemsSource = stats;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
