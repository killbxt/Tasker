using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class ProfileDialog : Window
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ObservableCollection<TaskViewModel> _tasks = new();

        public ProfileDialog(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _context = new ApplicationDbContext();

            TasksGrid.ItemsSource = _tasks;
            LoadProfile();
            LoadTasks();
        }

        private void LoadProfile()
        {
            var user = _authService.CurrentUser;
            if (user == null)
            {
                Close();
                return;
            }

            UsernameText.Text = user.Username;
            EmailText.Text = user.Email;

            var org = _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Members.Any(m => m.Id == user.Id));

            OrgText.Text = org != null ? $"Организация: {org.Name}" : "Организация: нет";
        }

        private void LoadTasks()
        {
            _tasks.Clear();

            var user = _authService.CurrentUser;
            if (user == null)
            {
                return;
            }

            var userId = user.Id;

            var tasks = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Where(t => t.TeamId == null && (t.AssignedToId == userId || t.CreatedById == userId))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.PlannedEndAt)
                .ToList();

            foreach (var task in tasks)
            {
                _tasks.Add(new TaskViewModel(task));
            }
        }

        /// <summary>Профиль показывает только личные задачи; в диалоге — только личная область.</summary>
        private static ObservableCollection<TeamFilterItem> PersonalWorkspaceOnly()
        {
            return new ObservableCollection<TeamFilterItem>
            {
                new TeamFilterItem { Name = "Личные задачи", Team = null }
            };
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var user = _authService.CurrentUser;
            if (user == null)
            {
                return;
            }

            var dialog = new TaskDialog(user, PersonalWorkspaceOnly(), null, selectedTeam: null);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                var task = dialog.ResultTask;
                task.CreatedById = user.Id;
                task.AssignedToId ??= user.Id;
                task.TeamId = null;
                _context.Tasks.Add(task);
                _context.SaveChanges();
                LoadTasks();
            }
        }

        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (TasksGrid.SelectedItem is not TaskViewModel taskVm)
            {
                return;
            }

            var user = _authService.CurrentUser;
            if (user == null)
            {
                return;
            }

            var dialog = new TaskDialog(user, PersonalWorkspaceOnly(), taskVm.Task, selectedTeam: null);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                taskVm.Title = dialog.ResultTask.Title;
                taskVm.Description = dialog.ResultTask.Description;
                taskVm.PlannedStartAt = dialog.ResultTask.PlannedStartAt;
                taskVm.PlannedEndAt = dialog.ResultTask.PlannedEndAt;
                taskVm.Priority = dialog.ResultTask.Priority;
                taskVm.Status = dialog.ResultTask.Status;
                taskVm.Task.TeamId = dialog.ResultTask.TeamId;
                taskVm.Task.AssignedToId = dialog.ResultTask.AssignedToId;

                taskVm.Task.UpdatedAt = DateTime.Now;
                if (taskVm.Task.Status == TaskState.Done)
                {
                    taskVm.Task.CompletedAt ??= DateTime.Now;
                }
                else
                {
                    taskVm.Task.CompletedAt = null;
                }

                _context.Entry(taskVm.Task).State = EntityState.Modified;
                _context.SaveChanges();
                LoadTasks();
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TasksGrid.SelectedItem is not TaskViewModel taskVm)
            {
                return;
            }

            if (MessageBox.Show($"Удалить задачу '{taskVm.Title}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            _context.Tasks.Remove(taskVm.Task);
            _context.SaveChanges();
            LoadTasks();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadTasks();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

