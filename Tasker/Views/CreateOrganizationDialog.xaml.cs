using System.Windows;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Views
{
    public partial class CreateOrganizationDialog : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public CreateOrganizationDialog(AuthService authService)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _authService = authService;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OrgNameTextBox.Text))
            {
                MessageBox.Show("Введите название организации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_authService.CurrentUser == null)
            {
                MessageBox.Show("Не удалось определить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ownerId = _authService.CurrentUser.Id;
            var existingOrgId = _context.Users.AsNoTracking()
                .Where(u => u.Id == ownerId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();
            if (existingOrgId != null)
            {
                MessageBox.Show(
                    "Вы уже состоите в организации. Сначала покиньте её: «Организация и команды» → «Покинуть организацию».",
                    "Уже в организации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var organization = new Organization
            {
                Name = OrgNameTextBox.Text,
                Description = OrgDescTextBox.Text,
                CreatedAt = DateTime.Now,
                OwnerId = ownerId
            };

            _context.Organizations.Add(organization);
            _context.SaveChanges();

            var user = _context.Users.Find(ownerId);
            if (user != null)
            {
                user.OrganizationId = organization.Id;
                _context.SaveChanges();
                _authService.RefreshCurrentUser();
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}