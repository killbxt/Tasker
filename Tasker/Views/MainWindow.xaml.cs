using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly AuthService _authService;
        // Invitations by token were removed; no background polling needed.

        public MainWindow(AuthService authService)
        {
            InitializeComponent();
            StateChanged += (_, _) => UpdateMaximizeGlyph();
            _authService = authService;
            _viewModel = new MainViewModel(authService);
            DataContext = _viewModel;

            _viewModel.AddTaskCommand = new RelayCommand(AddTask);
            _viewModel.EditTaskCommand = new RelayCommand<object>(param => EditTask(param as TaskViewModel));
            _viewModel.DeleteTaskCommand = new RelayCommand<object>(param => DeleteTask(param as TaskViewModel));
            _viewModel.ClearTodoColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.Todo));
            _viewModel.ClearInProgressColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.InProgress));
            _viewModel.ClearDoneColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.Done));
            _viewModel.ClearAllColumnsCommand = new RelayCommand(_viewModel.ClearAllColumns);
            _viewModel.OpenAIChatCommand = new RelayCommand(OpenAIChat);
            _viewModel.OpenProfileCommand = new RelayCommand(OpenProfile);
            _viewModel.OpenAnalyticsCommand = new RelayCommand(OpenAnalytics);
            _viewModel.OpenOverdueReportCommand = new RelayCommand(OpenOverdueReport);
            _viewModel.LogoutCommand = new RelayCommand(Logout);

            _viewModel.ManageTeamsCommand = new RelayCommand(ManageTeams);
            UpdateMaximizeGlyph();
        }

        private void UpdateMaximizeGlyph()
        {
            if (MaximizeRestoreIcon == null)
            {
                return;
            }

            MaximizeRestoreIcon.Kind = WindowState == WindowState.Maximized
                ? PackIconKind.WindowRestore
                : PackIconKind.WindowMaximize;
            MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Окно" : "Развернуть";
        }

        private void ChromeBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // Игнорируем, если окно уже в особом состоянии при старте перетаскивания
            }
        }

        private void WindowMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void WindowMaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void WindowClose_Click(object sender, RoutedEventArgs e) => Close();

        public void RefreshData()
        {
            _viewModel.LoadTeams();
            _viewModel.LoadTasks();
        }

        private void ManageTeams()
        {
            var dialog = new ManageTeamsDialog(_authService);
            dialog.ShowDialog();
            _viewModel.LoadTeams();
            _viewModel.LoadTasks();
        }
        private void AddTask()
        {
            var dialog = new TaskDialog(_authService.CurrentUser!, _viewModel.TeamFilters, null, _viewModel.SelectedTeamFilter?.Team);
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                _viewModel.AddTask(dialog.ResultTask);
            }
        }

        private void EditTask(TaskViewModel? task)
        {
            if (task == null)
            {
                return;
            }

            var dialog = new TaskDialog(_authService.CurrentUser!, _viewModel.TeamFilters, task.Task, _viewModel.SelectedTeamFilter?.Team);
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                var t = task.Task;
                var r = dialog.ResultTask;
                t.Title = r.Title;
                t.Description = r.Description;
                t.PlannedStartAt = r.PlannedStartAt;
                t.PlannedEndAt = r.PlannedEndAt;
                t.Priority = r.Priority;
                t.Status = r.Status;
                t.TeamId = r.TeamId;
                t.AssignedToId = r.AssignedToId;
                _viewModel.UpdateTask(task);
            }
        }

        private void DeleteTask(TaskViewModel? task)
        {
            if (task == null)
            {
                return;
            }

            if (MessageBox.Show($"Удалить задачу '{task.Title}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _viewModel.DeleteTask(task);
            }
        }

        private void OpenAIChat()
        {
            var chatDialog = new AIChatDialog(_viewModel, _authService);
            chatDialog.Owner = this;
            chatDialog.ShowDialog();
        }

        private void OpenProfile()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var dialog = new ProfileDialog(_authService);
            dialog.Owner = this;
            dialog.ShowDialog();

            _viewModel.LoadTasks();
        }

        private void OpenAnalytics()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var dialog = new AnalyticsDialog(_authService, _viewModel.SelectedTeamFilter?.Team);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void OpenOverdueReport()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var dialog = new OverdueReportDialog(_authService);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void Logout()
        {
            _authService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}