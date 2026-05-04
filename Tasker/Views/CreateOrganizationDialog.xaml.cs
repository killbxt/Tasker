using System.Windows;
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

            var organization = new Organization
            {
                Name = OrgNameTextBox.Text,
                Description = OrgDescTextBox.Text,
                CreatedAt = DateTime.Now
            };

            _context.Organizations.Add(organization);
            _context.SaveChanges();

            if (_authService.CurrentUser != null)
            {
                var user = _context.Users.Find(_authService.CurrentUser.Id);
                if (user != null)
                {
                    user.OrganizationId = organization.Id;
                    _context.SaveChanges();
                }
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