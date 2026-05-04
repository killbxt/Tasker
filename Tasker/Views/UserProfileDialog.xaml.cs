using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Data;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class UserProfileDialog : Window
    {
        private readonly int _userId;
        private readonly ApplicationDbContext _context;
        private readonly ObservableCollection<UserTaskRow> _rows = new();

        public UserProfileDialog(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _context = new ApplicationDbContext();

            TasksGrid.ItemsSource = _rows;
            LoadData();
        }

        private void LoadData()
        {
            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == _userId);
            if (user == null)
            {
                Close();
                return;
            }

            UsernameText.Text = user.Username;
            EmailText.Text = user.Email;

            _rows.Clear();

            var tasks = _context.Tasks
                .AsNoTracking()
                .Include(t => t.Team)
                .Where(t =>
                    (t.TeamId == null && (t.AssignedToId == _userId || t.CreatedById == _userId))
                    || (t.TeamId != null && t.AssignedToId == _userId))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.PlannedEndAt)
                .ToList();

            foreach (var t in tasks)
            {
                var vm = new TaskViewModel(t);
                _rows.Add(new UserTaskRow
                {
                    Title = vm.Title,
                    Status = vm.Status.ToString(),
                    Priority = vm.Priority.ToString(),
                    PlannedWindowText = vm.PlannedWindowText,
                    TeamName = t.Team?.Name ?? "Личное"
                });
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        public class UserTaskRow
        {
            public string Title { get; set; } = "";
            public string TeamName { get; set; } = "";
            public string Status { get; set; } = "";
            public string PlannedWindowText { get; set; } = "";
            public string Priority { get; set; } = "";
        }
    }
}

