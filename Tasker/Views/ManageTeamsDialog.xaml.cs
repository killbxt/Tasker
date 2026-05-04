using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class ManageTeamsDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private ObservableCollection<Team> UserTeams { get; set; }
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
        }

        private void LoadOrganization()
        {
            if (_authService.CurrentUser != null)
            {
                _myOrganization = _context.Organizations
                    .Include(o => o.Members)
                    .FirstOrDefault(o => o.Members.Any(m => m.Id == _authService.CurrentUser.Id));

                if (_myOrganization != null)
                {
                    OrganizationInfoText.Text = $"Организация: {_myOrganization.Name}\nУчастников: {_myOrganization.Members.Count}";
                    CreateOrgButton.Visibility = Visibility.Collapsed;
                    ManageMembersButton.Visibility = Visibility.Visible;
                }
                else
                {
                    OrganizationInfoText.Text = "У вас нет организации. Создайте организацию, чтобы начать.";
                    CreateOrgButton.Visibility = Visibility.Visible;
                    ManageMembersButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadUserTeams()
        {
            if (_authService.CurrentUser != null)
            {
                var currentUser = _context.Users
                    .Include(u => u.Teams)
                    .FirstOrDefault(u => u.Id == _authService.CurrentUser.Id);

                if (currentUser != null)
                {
                    UserTeams = new ObservableCollection<Team>(currentUser.Teams);
                    TeamsListBox.ItemsSource = UserTeams;
                }
            }
        }

        private void CreateOrganization_Click(object sender, RoutedEventArgs e)
        {
            var orgDialog = new CreateOrganizationDialog(_authService);
            if (orgDialog.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show("Организация создана! Теперь вы можете приглашать участников.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ManageMembers_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization != null)
            {
                var membersDialog = new ManageMembersDialog(_authService, _myOrganization);
                membersDialog.ShowDialog();
                LoadOrganization();
            }
        }

        private void CreateTeam_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrganization == null)
            {
                MessageBox.Show("Сначала создайте или вступите в организацию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TeamNameTextBox.Text))
            {
                MessageBox.Show("Введите название команды", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var team = new Team
            {
                Name = TeamNameTextBox.Text,
                Description = TeamDescTextBox.Text,
                OrganizationId = _myOrganization.Id,
                OwnerId = _authService.CurrentUser!.Id
            };

            _context.Teams.Add(team);
            _context.SaveChanges();

            if (_authService.CurrentUser != null)
            {
                var user = _context.Users.Find(_authService.CurrentUser.Id);
                if (user != null)
                {
                    team.Members.Add(user);
                    _context.SaveChanges();
                }
            }

            MessageBox.Show("Команда создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            TeamNameTextBox.Text = "";
            TeamDescTextBox.Text = "";
            LoadUserTeams();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}