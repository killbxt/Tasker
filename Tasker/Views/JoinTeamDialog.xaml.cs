using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class JoinTeamDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private ObservableCollection<Team> AvailableTeams { get; set; }

        public JoinTeamDialog(AuthService authService)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _authService = authService;

            LoadAvailableTeams();
        }

        private void LoadAvailableTeams()
        {
            if (_authService.CurrentUser != null)
            {
                var currentUserId = _authService.CurrentUser.Id;

                var teams = _context.Teams
                    .Include(t => t.Organization)
                    .Include(t => t.Members)
                    .Where(t => !t.Members.Any(m => m.Id == currentUserId))
                    .ToList();

                AvailableTeams = new ObservableCollection<Team>(teams);
                AvailableTeamsListBox.ItemsSource = AvailableTeams;
            }
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableTeamsListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите команду", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AvailableTeamsListBox.SelectedItem is Team selectedTeam && _authService.CurrentUser != null)
            {
                var team = _context.Teams.Include(t => t.Members).FirstOrDefault(t => t.Id == selectedTeam.Id);
                var user = _context.Users.Find(_authService.CurrentUser.Id);

                if (team != null && user != null)
                {
                    team.Members.Add(user);
                    _context.SaveChanges();
                    MessageBox.Show($"Вы вступили в команду {team.Name}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}