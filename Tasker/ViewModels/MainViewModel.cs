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
        public ObservableCollection<TeamFilterItem> TeamFilters { get; set; } = new();

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

        private TeamFilterItem? _selectedTeamFilter;
        private bool _suppressSelectedTeamFilterChanged;

        public TeamFilterItem? SelectedTeamFilter
        {
            get => _selectedTeamFilter;
            set
            {
                _selectedTeamFilter = value;
                OnPropertyChanged();
                RefreshTeamRosterText();
                if (!_suppressSelectedTeamFilterChanged)
                {
                    LoadTasks();
                }
            }
        }

        public ICommand AddTaskCommand { get; set; }
        public ICommand EditTaskCommand { get; set; }
        public ICommand DeleteTaskCommand { get; set; }
        public ICommand ClearTodoColumnCommand { get; set; }
        public ICommand ClearInProgressColumnCommand { get; set; }
        public ICommand ClearDoneColumnCommand { get; set; }
        public ICommand ClearAllColumnsCommand { get; set; }
        public ICommand OpenAIChatCommand { get; set; }
        public ICommand OpenProfileCommand { get; set; }
        public ICommand OpenAnalyticsCommand { get; set; }
        public ICommand OpenOverdueReportCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand ManageTeamsCommand { get; set; }
        public ICommand RefreshBoardCommand { get; set; }

        private string _teamRosterText = "";

        /// <summary>Список участников выбранной команды (имена через запятую).</summary>
        public string TeamRosterText => _teamRosterText;

        public bool HasTeamRoster => !string.IsNullOrEmpty(_teamRosterText);

        private bool _canModifyTasks = true;
        private bool _canUsePowerFeatures = true;

        /// <summary>
        /// Личные задачи — всегда можно править залогиненному; командная доска — только владельцу организации или капитану команды.
        /// Участник на доске команды может только перетаскивать свои карточки.
        /// </summary>
        public bool CanModifyTasks => _canModifyTasks;

        /// <summary>AI, аналитика, отчёт просрочек. У приглашённого в чужой организации — false.</summary>
        public bool CanUsePowerFeatures
        {
            get => _canUsePowerFeatures;
            private set
            {
                _canUsePowerFeatures = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Подсказка на командной доске для участника: только перетаскивание карточек.</summary>
        public bool ShowParticipantBoardHint => !CanModifyTasks && SelectedTeamFilter?.Team != null;

        public MainViewModel(AuthService authService)
        {
            _context = new ApplicationDbContext();
            _authService = authService;

            AddTaskCommand = new RelayCommand(() => { });
            EditTaskCommand = new RelayCommand<object>((p) => { });
            DeleteTaskCommand = new RelayCommand<object>((p) => { });
            ClearTodoColumnCommand = new RelayCommand(() => { });
            ClearInProgressColumnCommand = new RelayCommand(() => { });
            ClearDoneColumnCommand = new RelayCommand(() => { });
            ClearAllColumnsCommand = new RelayCommand(() => { });
            OpenAIChatCommand = new RelayCommand(() => { });
            OpenProfileCommand = new RelayCommand(() => { });
            OpenAnalyticsCommand = new RelayCommand(() => { });
            OpenOverdueReportCommand = new RelayCommand(() => { });
            LogoutCommand = new RelayCommand(() => { });
            ManageTeamsCommand = new RelayCommand(ManageTeams);
            RefreshBoardCommand = new RelayCommand(() => { });
            DropHandler = new DropHandler(this);
            LoadTeams();
        }

        private void RefreshWorkspacePermissions()
        {
            if (_authService.CurrentUser == null)
            {
                _canModifyTasks = false;
                OnPropertyChanged(nameof(CanModifyTasks));
                OnPropertyChanged(nameof(ShowParticipantBoardHint));
                CanUsePowerFeatures = false;
                return;
            }

            var uid = _authService.CurrentUser.Id;
            CanUsePowerFeatures = WorkspacePermissions.CanUsePowerFeatures(_context, uid);
            UpdateCanModifyTasks();
        }

        private bool ComputeCanModifyTasks()
        {
            if (_authService.CurrentUser == null)
            {
                return false;
            }

            var uid = _authService.CurrentUser.Id;
            var team = SelectedTeamFilter?.Team;
            if (team == null)
            {
                return WorkspacePermissions.CanModifyPersonalTasks(uid);
            }

            return WorkspacePermissions.CanModifyTeamBoard(_context, uid, team);
        }

        private void UpdateCanModifyTasks()
        {
            var v = ComputeCanModifyTasks();
            if (_canModifyTasks != v)
            {
                _canModifyTasks = v;
                OnPropertyChanged(nameof(CanModifyTasks));
            }

            OnPropertyChanged(nameof(ShowParticipantBoardHint));
        }

        private void ManageTeams()
        {
            var dialog = new ManageTeamsDialog(_authService);
            dialog.ShowDialog();
            LoadTeams();
        }

        private void SetTeamRosterText(string text)
        {
            if (_teamRosterText == text)
            {
                return;
            }

            _teamRosterText = text;
            OnPropertyChanged(nameof(TeamRosterText));
            OnPropertyChanged(nameof(HasTeamRoster));
        }

        private void RefreshTeamRosterText()
        {
            if (_authService.CurrentUser == null)
            {
                SetTeamRosterText("");
                return;
            }

            var team = SelectedTeamFilter?.Team;
            if (team?.Members == null || team.Members.Count == 0)
            {
                SetTeamRosterText("");
                return;
            }

            var names = string.Join(", ", team.Members.OrderBy(m => m.Username).Select(m => m.Username));
            SetTeamRosterText($"Участники: {names}");
        }

        public void LoadTeams()
        {
            RefreshWorkspacePermissions();

            if (_authService.CurrentUser == null)
            {
                SetTeamRosterText("");
                return;
            }

            var currentUserId = _authService.CurrentUser.Id;
            var teams = _context.Teams
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .Where(t => t.Members.Any(m => m.Id == currentUserId))
                .ToList();

            var previousSelectedTeamId = SelectedTeamFilter?.Team?.Id;

            _suppressSelectedTeamFilterChanged = true;
            try
            {
                TeamFilters.Clear();
                TeamFilters.Add(new TeamFilterItem { Name = "Личные задачи", Team = null });
                foreach (var team in teams.OrderBy(t => t.Name))
                {
                    TeamFilters.Add(new TeamFilterItem { Name = team.Name, Team = team });
                }

                _selectedTeamFilter =
                    TeamFilters.FirstOrDefault(t => t.Team?.Id == previousSelectedTeamId)
                    ?? TeamFilters.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedTeamFilter));
            }
            finally
            {
                _suppressSelectedTeamFilterChanged = false;
            }

            RefreshTeamRosterText();
            LoadTasks();
        }

        public void LoadTasks()
        {
            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();

            _context.ChangeTracker.Clear();

            var query = _context.Tasks
                .AsNoTracking()
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.Team)
                .AsQueryable();

            if (_authService.CurrentUser != null)
            {
                var currentUserId = _authService.CurrentUser.Id;

                var selectedTeam = SelectedTeamFilter?.Team;
                if (selectedTeam != null)
                {
                    var teamId = selectedTeam.Id;
                    var isOrgOwner = _context.Organizations.AsNoTracking()
                        .Any(o => o.Id == selectedTeam.OrganizationId && o.OwnerId == currentUserId);
                    var isTeamOwner = selectedTeam.OwnerId == currentUserId;
                    if (isOrgOwner || isTeamOwner)
                    {
                        query = query.Where(t => t.TeamId == teamId);
                    }
                    else
                    {
                        query = query.Where(t => t.TeamId == teamId && t.AssignedToId == currentUserId);
                    }
                }
                else
                {
                    // Personal scope: only tasks not tied to a team.
                    query = query.Where(t => t.TeamId == null && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
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

            UpdateCanModifyTasks();
        }

        public void AddTask(Models.Task task)
        {
            task.CreatedById = _authService.CurrentUser!.Id;
            task.AssignedToId ??= _authService.CurrentUser!.Id;
            task.PlannedEndAt = task.PlannedEndAt == default ? DateTime.Now.AddDays(7) : task.PlannedEndAt;
            _context.Tasks.Add(task);
            _context.SaveChanges();
            LoadTasks();
        }

        public void UpdateTask(TaskViewModel taskVm)
        {
            var s = taskVm.Task;
            s.UpdatedAt = DateTime.Now;
            if (s.Status == TaskState.Done)
            {
                s.CompletedAt ??= DateTime.Now;
            }
            else
            {
                s.CompletedAt = null;
            }

            var entity = _context.Tasks.Find(s.Id);
            if (entity == null)
            {
                LoadTasks();
                return;
            }

            entity.Title = s.Title;
            entity.Description = s.Description;
            entity.Status = s.Status;
            entity.Priority = s.Priority;
            entity.PlannedStartAt = s.PlannedStartAt;
            entity.PlannedEndAt = s.PlannedEndAt;
            entity.TeamId = s.TeamId;
            entity.AssignedToId = s.AssignedToId;
            entity.UpdatedAt = s.UpdatedAt;
            entity.CompletedAt = s.CompletedAt;

            _context.SaveChanges();
            LoadTasks();
        }

        public void DeleteTask(TaskViewModel taskVm)
        {
            var entity = _context.Tasks.Find(taskVm.Task.Id);
            if (entity != null)
            {
                _context.Tasks.Remove(entity);
            }

            _context.SaveChanges();
            LoadTasks();
        }

        public void ClearColumn(TaskState status)
        {
            if (_authService.CurrentUser == null || !CanModifyTasks)
            {
                return;
            }

            var currentUserId = _authService.CurrentUser.Id;
            var selectedTeam = SelectedTeamFilter?.Team;
            IQueryable<Models.Task> tasksToDelete;

            if (selectedTeam != null)
            {
                var isOrgOwner = _context.Organizations.AsNoTracking()
                    .Any(o => o.Id == selectedTeam.OrganizationId && o.OwnerId == currentUserId);
                var canManageAllTeam = isOrgOwner || selectedTeam.OwnerId == currentUserId;
                if (canManageAllTeam)
                {
                    tasksToDelete = status switch
                    {
                        TaskState.Todo => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.Todo),
                        TaskState.InProgress => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.InProgress),
                        _ => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.Done)
                    };
                }
                else
                {
                    tasksToDelete = status switch
                    {
                        TaskState.Todo => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.Todo && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId)),
                        TaskState.InProgress => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.InProgress && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId)),
                        _ => _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && t.Status == TaskState.Done && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId))
                    };
                }
            }
            else
            {
                tasksToDelete = status switch
                {
                    TaskState.Todo => _context.Tasks.Where(t => t.TeamId == null && t.Status == TaskState.Todo && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId)),
                    TaskState.InProgress => _context.Tasks.Where(t => t.TeamId == null && t.Status == TaskState.InProgress && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId)),
                    _ => _context.Tasks.Where(t => t.TeamId == null && t.Status == TaskState.Done && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId))
                };
            }

            _context.Tasks.RemoveRange(tasksToDelete);
            _context.SaveChanges();
            LoadTasks();
        }

        public void ClearAllColumns()
        {
            if (_authService.CurrentUser == null || !CanModifyTasks)
            {
                return;
            }

            var currentUserId = _authService.CurrentUser.Id;
            var selectedTeam = SelectedTeamFilter?.Team;
            IQueryable<Models.Task> userTasks;
            if (selectedTeam != null)
            {
                var isOrgOwner = _context.Organizations.AsNoTracking()
                    .Any(o => o.Id == selectedTeam.OrganizationId && o.OwnerId == currentUserId);
                var canManageAllTeam = isOrgOwner || selectedTeam.OwnerId == currentUserId;
                userTasks = canManageAllTeam
                    ? _context.Tasks.Where(t => t.TeamId == selectedTeam.Id)
                    : _context.Tasks.Where(t => t.TeamId == selectedTeam.Id && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
            }
            else
            {
                userTasks = _context.Tasks.Where(t => t.TeamId == null && (t.AssignedToId == currentUserId || t.CreatedById == currentUserId));
            }

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