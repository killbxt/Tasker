using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class ManageMembersDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly Organization _currentOrganization;
        private ObservableCollection<User> Members { get; set; }
        private ObservableCollection<Team> Teams { get; set; }

        public ManageMembersDialog(AuthService authService, Organization organization)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _authService = authService;
            _currentOrganization = organization;

            if (_authService.CurrentUser == null || organization.OwnerId != _authService.CurrentUser.Id)
            {
                MessageBox.Show(
                    "Управлять участниками может только владелец организации.",
                    "Нет доступа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Close();
                return;
            }

            LoadMembers();
            LoadTeams();
        }

        private void LoadMembers()
        {
            var org = _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Id == _currentOrganization.Id);

            if (org != null)
            {
                Members = new ObservableCollection<User>(org.Members);
                MembersListBox.ItemsSource = Members;
            }
        }

        private void LoadTeams()
        {
            var teams = _context.Teams
                .Where(t => t.OrganizationId == _currentOrganization.Id)
                .ToList();

            Teams = new ObservableCollection<Team>(teams);
            TeamComboBox.ItemsSource = Teams;
        }

        private async void SendInvitation_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var email = EmailTextBox.Text.Trim();
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                MessageBox.Show("Пользователь с таким email не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            user.OrganizationId = _currentOrganization.Id;

            string teamMessage = "";
            if (TeamComboBox.SelectedItem is Team selectedTeam)
            {
                var team = _context.Teams
                    .Include(t => t.Members)
                    .FirstOrDefault(t => t.Id == selectedTeam.Id);

                if (team != null && !team.Members.Any(m => m.Id == user.Id))
                {
                    team.Members.Add(user);
                    teamMessage = $" и в команду {team.Name}";
                }
            }

            await _context.SaveChangesAsync();

            if (_authService.CurrentUser != null && user.Id == _authService.CurrentUser.Id)
            {
                _authService.RefreshCurrentUser();
            }

            MessageBox.Show($"Пользователь добавлен в организацию{teamMessage}.",
                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

            EmailTextBox.Text = "";
            TeamComboBox.SelectedItem = null;
            LoadMembers();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            if (MembersListBox.SelectedItem is not User member)
            {
                MessageBox.Show("Выберите участника", "Профиль", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new UserProfileDialog(member.Id);
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}