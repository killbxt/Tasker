using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class ManageTeamsDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private ObservableCollection<Team> UserTeams { get; set; } = new();
        private Organization? _myOrganization;

        public ManageTeamsDialog(AuthService authService)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _authService = authService;
            LoadData();
        }

        private void LoadData()
        {
            LoadOrganization();
            LoadUserTeams();
            UpdateTeamFormState();
        }

        private void LoadOrganization()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            _myOrganization = _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Members.Any(m => m.Id == _authService.CurrentUser.Id));

            if (_myOrganization != null)
            {
                NoOrgPanel.Visibility = Visibility.Collapsed;
                HasOrgPanel.Visibility = Visibility.Visible;
                OrganizationInfoText.Text =
                    $"Участников в организации: {_myOrganization.Members.Count}. " +
                    "Любой участник может изменить название и описание; удаление — тоже у всех (будьте осторожны).";
                OrgNameDisplay.Text = _myOrganization.Name;
                OrgDescDisplay.Text = string.IsNullOrWhiteSpace(_myOrganization.Description)
                    ? "Без описания"
                    : _myOrganization.Description;
                OrgNameEditBox.Text = _myOrganization.Name;
                OrgDescEditBox.Text = _myOrganization.Description;
            }
            else
            {
                NoOrgPanel.Visibility = Visibility.Visible;
                HasOrgPanel.Visibility = Visibility.Collapsed;
                _myOrganization = null;
            }
        }

        private void LoadUserTeams()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var currentUser = _context.Users
                .Include(u => u.Teams)
                .FirstOrDefault(u => u.Id == _authService.CurrentUser.Id);

            if (currentUser != null)
            {
                UserTeams = new ObservableCollection<Team>(
                    currentUser.Teams.OrderBy(t => t.Name));
                TeamsListBox.ItemsSource = UserTeams;
            }
        }

        private void UpdateTeamFormState()
        {
            var hasOrg = _myOrganization != null;
            TeamNameTextBox.IsEnabled = hasOrg;
            TeamDescTextBox.IsEnabled = hasOrg;
        }

        private void CreateOrganization_Click(object sender, RoutedEventArgs e)
        {
            var orgDialog = new CreateOrganizationDialog(_authService);
            if (orgDialog.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show(
                    "Организация создана. Перейдите во вкладку «Команды», чтобы добавить первую команду.",
                    "Готово",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ManageMembers_Click(object sender, RoutedEventArgs e) => OpenMembersDialog();

        private void ManageMembersFromTab_Click(object sender, RoutedEventArgs e) => OpenMembersDialog();

        private void OpenMembersDialog()
        {
            if (_myOrganization == null)
            {
                MessageBox.Show(
                    "Сначала создайте организацию на первой вкладке.",
                    "Нет организации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var membersDialog = new ManageMembersDialog(_authService, _myOrganization);
            membersDialog.Owner = this;
            membersDialog.ShowDialog();
            LoadOrganization();
            LoadUserTeams();
        }

        private void StartEditOrganization_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization == null)
            {
                return;
            }

            OrgViewPanel.Visibility = Visibility.Collapsed;
            OrgEditPanel.Visibility = Visibility.Visible;
            OrgNameEditBox.Text = _myOrganization.Name;
            OrgDescEditBox.Text = _myOrganization.Description;
        }

        private void CancelOrganizationEdit_Click(object sender, RoutedEventArgs e)
        {
            OrgEditPanel.Visibility = Visibility.Collapsed;
            OrgViewPanel.Visibility = Visibility.Visible;
        }

        private void SaveOrganizationEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization == null || _authService.CurrentUser == null)
            {
                return;
            }

            if (!WorkspaceAdminService.TryUpdateOrganization(
                    _context,
                    _authService.CurrentUser.Id,
                    _myOrganization.Id,
                    OrgNameEditBox.Text,
                    OrgDescEditBox.Text,
                    out var error))
            {
                MessageBox.Show(error, "Не удалось сохранить", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OrgEditPanel.Visibility = Visibility.Collapsed;
            OrgViewPanel.Visibility = Visibility.Visible;
            LoadOrganization();
            MessageBox.Show("Изменения сохранены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteOrganization_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization == null || _authService.CurrentUser == null)
            {
                return;
            }

            var r = MessageBox.Show(
                "Удалить организацию? Все команды внутри исчезнут, задачи команд станут без команды (личными по сути). " +
                "У участников пропадёт привязка к организации. Это действие нельзя отменить.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes)
            {
                return;
            }

            if (!WorkspaceAdminService.TryDeleteOrganization(_context, _authService.CurrentUser.Id, _myOrganization.Id, out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _myOrganization = null;
            LoadData();
            MessageBox.Show("Организация удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TeamsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            if (TeamsListBox.SelectedItem is not Team team)
            {
                TeamDetailPanel.Visibility = Visibility.Collapsed;
                TeamPickHint.Visibility = Visibility.Visible;
                return;
            }

            TeamPickHint.Visibility = Visibility.Collapsed;
            TeamDetailPanel.Visibility = Visibility.Visible;

            var teamTracked = _context.Teams.First(t => t.Id == team.Id);
            SelectedTeamNameBox.Text = teamTracked.Name;
            SelectedTeamDescBox.Text = teamTracked.Description;

            var isOwner = teamTracked.OwnerId == _authService.CurrentUser.Id;
            SaveTeamButton.IsEnabled = isOwner;
            DeleteTeamButton.IsEnabled = isOwner;
            SelectedTeamNameBox.IsEnabled = isOwner;
            SelectedTeamDescBox.IsEnabled = isOwner;

            TeamOwnerHint.Text = isOwner
                ? "Вы владелец: можно переименовать или удалить команду."
                : "Вы участник: редактировать и удалять может только владелец.";
        }

        private void SaveTeam_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsListBox.SelectedItem is not Team team || _authService.CurrentUser == null)
            {
                return;
            }

            if (!WorkspaceAdminService.TryUpdateTeam(
                    _context,
                    _authService.CurrentUser.Id,
                    team.Id,
                    SelectedTeamNameBox.Text,
                    SelectedTeamDescBox.Text,
                    out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadUserTeams();
            TeamsListBox.SelectedItem = UserTeams.FirstOrDefault(t => t.Id == team.Id);
            MessageBox.Show("Команда обновлена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteTeam_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsListBox.SelectedItem is not Team team || _authService.CurrentUser == null)
            {
                return;
            }

            if (MessageBox.Show(
                    $"Удалить команду «{team.Name}»? Задачи этой команды останутся, но без привязки к команде.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            if (!WorkspaceAdminService.TryDeleteTeam(_context, _authService.CurrentUser.Id, team.Id, out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TeamDetailPanel.Visibility = Visibility.Collapsed;
            TeamPickHint.Visibility = Visibility.Visible;
            TeamsListBox.SelectedItem = null;
            LoadUserTeams();
            MessageBox.Show("Команда удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateTeam_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization == null)
            {
                MessageBox.Show("Сначала создайте организацию (первая вкладка).", "Нет организации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_authService.CurrentUser == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(TeamNameTextBox.Text))
            {
                MessageBox.Show("Введите название команды.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!WorkspaceAdminService.TryCreateTeam(
                    _context,
                    _authService.CurrentUser.Id,
                    _myOrganization.Id,
                    TeamNameTextBox.Text,
                    TeamDescTextBox.Text,
                    out var team,
                    out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Команда создана.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            TeamNameTextBox.Text = "";
            TeamDescTextBox.Text = "";
            LoadUserTeams();
            if (team != null)
            {
                TeamsListBox.SelectedItem = UserTeams.FirstOrDefault(t => t.Id == team.Id);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
