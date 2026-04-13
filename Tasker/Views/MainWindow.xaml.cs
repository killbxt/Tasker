using System.Windows;
using System.Windows.Input;
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
        private System.Timers.Timer _invitationChecker;

        public MainWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _viewModel = new MainViewModel(authService);
            DataContext = _viewModel;

            // Переопределяем команды
            _viewModel.AddTaskCommand = new RelayCommand(AddTask);
            _viewModel.EditTaskCommand = new RelayCommand<object>(param => EditTask(param as TaskViewModel));
            _viewModel.DeleteTaskCommand = new RelayCommand<object>(param => DeleteTask(param as TaskViewModel));
            _viewModel.ClearTodoColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.Todo));
            _viewModel.ClearInProgressColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.InProgress));
            _viewModel.ClearDoneColumnCommand = new RelayCommand(() => _viewModel.ClearColumn(TaskState.Done));
            _viewModel.ClearAllColumnsCommand = new RelayCommand(_viewModel.ClearAllColumns);
            _viewModel.OpenAIChatCommand = new RelayCommand(OpenAIChat);
            _viewModel.LogoutCommand = new RelayCommand(Logout);

            _viewModel.ManageTeamsCommand = new RelayCommand(ManageTeams);
        }
        private void StartInvitationChecker()
        {
            _invitationChecker = new System.Timers.Timer(30000); // Проверяем каждые 30 секунд
            _invitationChecker.Elapsed += async (s, e) => await CheckNewInvitations();
            _invitationChecker.Start();
        }
        private async System.Threading.Tasks.Task CheckNewInvitations()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            using var context = new ApplicationDbContext();
            var pendingInvites = context.Invitations
                .Where(i => i.Email == _authService.CurrentUser.Email && !i.IsUsed && i.ExpiresAt > DateTime.Now)
                .ToList();

            foreach (var invite in pendingInvites)
            {
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show($"Вас пригласили в организацию!\n\nКод: {invite.Token}\n\nПринять приглашение?",
                        "Новое приглашение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var acceptDialog = new AcceptInvitationDialog(_authService, invite.Token);
                        acceptDialog.ShowDialog();
                    }
                });
            }
        }
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
            var dialog = new TaskDialog(_authService.CurrentUser!, _viewModel.UserTeams, null, _viewModel.SelectedTeam);
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

            var dialog = new TaskDialog(_authService.CurrentUser!, _viewModel.UserTeams, task.Task, _viewModel.SelectedTeam);
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                task.Title = dialog.ResultTask.Title;
                task.Description = dialog.ResultTask.Description;
                task.DueDate = dialog.ResultTask.DueDate;
                task.Priority = dialog.ResultTask.Priority;
                task.Status = dialog.ResultTask.Status;
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

        private async void OpenAIChat()
        {
            var chatDialog = new AIChatDialog(_viewModel);
            chatDialog.ShowDialog();
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