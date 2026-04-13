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

            // Проверяем, существует ли пользователь
            var user = _context.Users.FirstOrDefault(u => u.Email == EmailTextBox.Text);
            if (user == null)
            {
                MessageBox.Show("Пользователь с таким email не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, не состоит ли уже в организации
            if (user.OrganizationId == _currentOrganization.Id)
            {
                MessageBox.Show("Пользователь уже состоит в этой организации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем инвайт
            var invitation = new Invitation
            {
                Email = EmailTextBox.Text,
                OrganizationId = _currentOrganization.Id,
                TeamId = (TeamComboBox.SelectedItem as Team)?.Id,
                InvitedById = _authService.CurrentUser!.Id,
                ExpiresAt = DateTime.Now.AddDays(7)
            };

            _context.Invitations.Add(invitation);
            await _context.SaveChangesAsync();

            MessageBox.Show($"Приглашение отправлено на {EmailTextBox.Text}\n\nТокен: {invitation.Token}",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            EmailTextBox.Text = "";
            TeamComboBox.SelectedItem = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}