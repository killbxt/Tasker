using Microsoft.EntityFrameworkCore;
using System.Windows;
using TaskManager.Data;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class AcceptInvitationDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AcceptInvitationDialog(AuthService authService, string? token = null)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _authService = authService;

            if (!string.IsNullOrWhiteSpace(token))
            {
                TokenTextBox.Text = token;
            }

            InviteInfoText.Text = "Владелец организации приглашает вас присоединиться.\nВведите код приглашения ниже или нажмите Отклонить чтобы отказаться.";
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            var token = TokenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Введите код приглашения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invitation = await _context.Invitations
                .Include(i => i.Organization)
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Token == token && !i.IsUsed && i.ExpiresAt > DateTime.Now);

            if (invitation == null)
            {
                MessageBox.Show("Недействительный или просроченный код приглашения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_authService.CurrentUser == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var currentUser = await _context.Users.FindAsync(_authService.CurrentUser.Id);
            if (currentUser == null)
            {
                return;
            }

            // Проверяем email
            if (currentUser.Email != invitation.Email)
            {
                MessageBox.Show($"Это приглашение предназначено для {invitation.Email}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем пользователя в организацию
            currentUser.OrganizationId = invitation.OrganizationId;

            // Добавляем в команду, если указана
            if (invitation.TeamId.HasValue)
            {
                var team = await _context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == invitation.TeamId);
                if (team != null && !team.Members.Any(m => m.Id == currentUser.Id))
                {
                    team.Members.Add(currentUser);
                }
            }

            invitation.IsUsed = true;
            await _context.SaveChangesAsync();

            MessageBox.Show($"Добро пожаловать в организацию {invitation.Organization.Name}!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите отклонить приглашение?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}