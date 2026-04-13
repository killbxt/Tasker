using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.Views;

namespace TaskManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        public IDropTarget DropHandler { get; private set; }
        public ObservableCollection<TaskViewModel> TodoTasks { get; set; } = new();
        public ObservableCollection<TaskViewModel> InProgressTasks { get; set; } = new();
        public ObservableCollection<TaskViewModel> DoneTasks { get; set; } = new();
        public ObservableCollection<Team> UserTeams { get; set; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadTasks();
            }
        }

        private Team? _selectedTeam;
        public Team? SelectedTeam
        {
            get => _selectedTeam;
            set
            {
                _selectedTeam = value;
                OnPropertyChanged();
                LoadTasks();
            }
        }

        // Команды
        public ICommand AddTaskCommand { get; set; }
        public ICommand EditTaskCommand { get; set; }
        public ICommand DeleteTaskCommand { get; set; }
        public ICommand ClearTodoColumnCommand { get; set; }
        public ICommand ClearInProgressColumnCommand { get; set; }
        public ICommand ClearDoneColumnCommand { get; set; }
        public ICommand ClearAllColumnsCommand { get; set; }
        public ICommand OpenAIChatCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand ManageTeamsCommand { get; set; }
        public MainViewModel(AuthService authService)
        {
            _context = new ApplicationDbContext();
            _authService = authService;

            // Инициализация команд с заглушками
            AddTaskCommand = new RelayCommand(() => { });
            EditTaskCommand = new RelayCommand<object>((p) => { });
            DeleteTaskCommand = new RelayCommand<object>((p) => { });
            ClearTodoColumnCommand = new RelayCommand(() => { });
            ClearInProgressColumnCommand = new RelayCommand(() => { });
            ClearDoneColumnCommand = new RelayCommand(() => { });
            ClearAllColumnsCommand = new RelayCommand(() => { });
            OpenAIChatCommand = new RelayCommand(() => { });
            LogoutCommand = new RelayCommand(() => { });
            ManageTeamsCommand = new RelayCommand(ManageTeams);
            DropHandler = new DropHandler(this);
            LoadTeams();
            LoadTasks();
        }

        private void ManageTeams()
        {
            var dialog = new ManageTeamsDialog(_authService);
            dialog.ShowDialog();
            LoadTeams(); // Обновляем список команд
            LoadTasks(); // Обновляем задачи
        }

        public void LoadTeams()
        {
            if (_authService.CurrentUser != null)
            {
                var currentUserId = _authService.CurrentUser.Id;
                var teams = _context.Teams
                    .Include(t => t.Members)
                    .Where(t => t.Members.Any(m => m.Id == currentUserId))
                    .ToList();

                UserTeams.Clear();
                foreach (var team in teams)
                {
                    UserTeams.Add(team);
                }
            }
        }

        public void LoadTasks()
        {
            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();

            var query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .AsQueryable();

            if (_authService.CurrentUser != null)
            {
                var currentUserId = _authService.CurrentUser.Id;

                if (SelectedTeam != null)
                {
                    var teamId = SelectedTeam.Id;
                    query = query.Where(t => t.TeamId == teamId);
                }
                else
                {
                    // Исправлено: без оператора ?.
                    query = query.Where(t => t.AssignedToId == currentUserId || t.CreatedById == currentUserId);
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchText = SearchText;
                query = query.Where(t => t.Title.Contains(searchText) || t.Description.Contains(searchText));
            }

            var tasks = query.ToList();

            foreach (var task in tasks)
            {
                var vm = new TaskViewModel(task);
                switch (task.Status)
                {
                    case TaskState.Todo:
                        TodoTasks.Add(vm);
                        break;
                    case TaskState.InProgress:
                        InProgressTasks.Add(vm);
                        break;
                    case TaskState.Done:
                        DoneTasks.Add(vm);
                        break;
                }
            }
        }

        public void AddTask(Models.Task task)
        {
            task.CreatedById = _authService.CurrentUser!.Id;
            _context.Tasks.Add(task);
            _context.SaveChanges();
            LoadTasks();
        }

        public void UpdateTask(TaskViewModel taskVm)
        {
            taskVm.Task.UpdatedAt = DateTime.Now;
            _context.Entry(taskVm.Task).State = EntityState.Modified;
            _context.SaveChanges();
            LoadTasks();
        }

        public void DeleteTask(TaskViewModel taskVm)
        {
            _context.Tasks.Remove(taskVm.Task);
            _context.SaveChanges();
            LoadTasks();
        }

        public void ClearColumn(TaskState status)
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var currentUserId = _authService.CurrentUser.Id;
            IQueryable<Models.Task> tasksToDelete;

            if (status == TaskState.Todo)
            {
                tasksToDelete = _context.Tasks.Where(t => t.Status == TaskState.Todo && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
            }
            else if (status == TaskState.InProgress)
            {
                tasksToDelete = _context.Tasks.Where(t => t.Status == TaskState.InProgress && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
            }
            else
            {
                tasksToDelete = _context.Tasks.Where(t => t.Status == TaskState.Done && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
            }

            _context.Tasks.RemoveRange(tasksToDelete);
            _context.SaveChanges();
            LoadTasks();
        }

        public void ClearAllColumns()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var currentUserId = _authService.CurrentUser.Id;
            var userTasks = _context.Tasks.Where(t => t.AssignedToId == currentUserId || t.CreatedById == currentUserId);
            _context.Tasks.RemoveRange(userTasks);
            _context.SaveChanges();
            LoadTasks();
        }

        public void MoveTask(TaskViewModel task, TaskState newStatus)
        {
            task.Status = newStatus;
            UpdateTask(task);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}