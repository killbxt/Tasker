using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class AIChatDialog : Window
    {
        private readonly AIChatService _aiService;
        private readonly MainViewModel _viewModel;
        private readonly AuthService _authService;
        private readonly ObservableCollection<ChatMessage> _messages;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public AIChatDialog(MainViewModel viewModel, AuthService authService)
        {
            InitializeComponent();
            _aiService = new AIChatService();
            _viewModel = viewModel;
            _authService = authService;
            _messages = new ObservableCollection<ChatMessage>();
            MessagesListBox.ItemsSource = _messages;

            _messages.Add(new ChatMessage
            {
                Text = "Привет! Я помогаю с задачами и с организацией.\n\n" +
                       "Задачи: создать, изменить, удалить по описанию.\n" +
                       "Организация: «переименуй организацию в …», «удали организацию».\n" +
                       "Команды: «создай команду …», «переименуй команду 2 в …», «удали команду …».\n\n" +
                       "Примеры: «Создай задачу отчёт до пятницы 18:00», «Удали задачу номер 3».",
                IsUser = false
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendMessage();

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                await SendMessage();
            }
        }

        private string BuildWorkspaceContextJson()
        {
            if (_authService.CurrentUser == null)
            {
                return "{}";
            }

            using var db = new ApplicationDbContext();
            var userId = _authService.CurrentUser.Id;

            var org = db.Organizations
                .AsNoTracking()
                .Include(o => o.Members)
                .FirstOrDefault(o => o.Members.Any(m => m.Id == userId));

            var teams = db.Teams
                .AsNoTracking()
                .Include(t => t.Members)
                .Where(t => t.Members.Any(m => m.Id == userId))
                .Select(t => new { t.Id, t.Name, t.Description, t.OrganizationId, t.OwnerId })
                .ToList();

            var payload = new
            {
                userId,
                organization = org == null ? null : new { org.Id, org.Name, org.Description },
                teams
            };

            return JsonSerializer.Serialize(payload);
        }

        private static string NormalizeAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "none";
            }

            return action.Trim().ToLowerInvariant() switch
            {
                "create" => "create_task",
                "update" => "update_task",
                "delete" => "delete_task",
                var a => a
            };
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            if (_authService.CurrentUser == null)
            {
                return;
            }

            var message = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _messages.Add(new ChatMessage { Text = message, IsUser = true });
            MessageTextBox.Clear();

            var loadingMsg = new ChatMessage { Text = "Думаю…", IsUser = false, IsLoading = true };
            _messages.Add(loadingMsg);
            await System.Threading.Tasks.Task.Delay(100);
            ScrollToBottom();

            var tasks = new List<Models.Task>();
            foreach (var taskVm in _viewModel.TodoTasks)
            {
                tasks.Add(taskVm.Task);
            }

            foreach (var taskVm in _viewModel.InProgressTasks)
            {
                tasks.Add(taskVm.Task);
            }

            foreach (var taskVm in _viewModel.DoneTasks)
            {
                tasks.Add(taskVm.Task);
            }

            var workspaceJson = BuildWorkspaceContextJson();
            var response = await _aiService.SendMessageAsync(message, tasks, workspaceJson);

            Application.Current.Dispatcher.Invoke(() => _messages.Remove(loadingMsg));

            try
            {
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonResponse = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var aiResponse = JsonSerializer.Deserialize<AIChatResponse>(jsonResponse, JsonOpts);

                    if (aiResponse != null)
                    {
                        var action = NormalizeAction(aiResponse.action);
                        var handled = await ApplyAiActionAsync(action, aiResponse);
                        if (!handled)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _messages.Add(new ChatMessage
                                {
                                    Text = aiResponse.message ?? "Понял! Чем ещё помочь?",
                                    IsUser = false
                                });
                            });
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(response))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = response, IsUser = false });
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = "Не удалось получить ответ от сервера.", IsUser = false });
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _messages.Add(new ChatMessage
                    {
                        Text = $"Ошибка разбора ответа: {ex.Message}",
                        IsUser = false
                    });
                });
            }

            ScrollToBottom();
        }

        private async System.Threading.Tasks.Task<bool> ApplyAiActionAsync(string action, AIChatResponse ai)
        {
            await System.Threading.Tasks.Task.Yield();

            if (_authService.CurrentUser == null)
            {
                return false;
            }

            var uid = _authService.CurrentUser.Id;
            using var db = new ApplicationDbContext();

            switch (action)
            {
                case "create_task" when ai.taskData != null:
                {
                    var newTask = new Models.Task
                    {
                        Title = ai.taskData.title ?? "Новая задача",
                        Description = ai.taskData.description ?? "",
                        PlannedStartAt = DateTime.TryParse(ai.taskData.plannedStartAt, out var startAt) ? startAt : null,
                        PlannedEndAt = DateTime.TryParse(ai.taskData.plannedEndAt, out var endAt) ? endAt : DateTime.Now.AddDays(7),
                        Priority = ai.taskData.priority == "Urgent" ? TaskPriority.Urgent : TaskPriority.Normal,
                        Status = TaskState.Todo,
                        CreatedAt = DateTime.Now
                    };
                    _viewModel.AddTask(newTask);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nЗадача создана.", IsUser = false });
                    });
                    return true;
                }

                case "update_task" when ai.taskId.HasValue && ai.taskData != null:
                {
                    var taskToUpdate = FindTaskById(ai.taskId.Value);
                    if (taskToUpdate == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = $"Не найдена задача с ID {ai.taskId}.", IsUser = false });
                        });
                        return true;
                    }

                    if (!string.IsNullOrEmpty(ai.taskData.title))
                    {
                        taskToUpdate.Title = ai.taskData.title;
                    }

                    if (!string.IsNullOrEmpty(ai.taskData.description))
                    {
                        taskToUpdate.Description = ai.taskData.description;
                    }

                    if (ai.taskData.priority != null)
                    {
                        taskToUpdate.Priority = ai.taskData.priority == "Urgent" ? TaskPriority.Urgent : TaskPriority.Normal;
                    }

                    if (!string.IsNullOrEmpty(ai.taskData.plannedStartAt) &&
                        DateTime.TryParse(ai.taskData.plannedStartAt, out var newStartAt))
                    {
                        taskToUpdate.PlannedStartAt = newStartAt;
                    }

                    if (!string.IsNullOrEmpty(ai.taskData.plannedEndAt) &&
                        DateTime.TryParse(ai.taskData.plannedEndAt, out var newEndAt))
                    {
                        taskToUpdate.PlannedEndAt = newEndAt;
                    }

                    _viewModel.UpdateTask(taskToUpdate);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nЗадача обновлена.", IsUser = false });
                    });
                    return true;
                }

                case "delete_task" when ai.taskId.HasValue:
                {
                    var taskToDelete = FindTaskById(ai.taskId.Value);
                    if (taskToDelete == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = $"Не найдена задача с ID {ai.taskId}.", IsUser = false });
                        });
                        return true;
                    }

                    var title = taskToDelete.Title;
                    _viewModel.DeleteTask(taskToDelete);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nЗадача «{title}» удалена.", IsUser = false });
                    });
                    return true;
                }

                case "update_organization" when ai.organizationData != null:
                {
                    var org = db.Organizations
                        .Include(o => o.Members)
                        .FirstOrDefault(o => o.Members.Any(m => m.Id == uid));

                    if (org == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = "Нет организации для изменения.", IsUser = false });
                        });
                        return true;
                    }

                    var name = ai.organizationData.name ?? org.Name;
                    var desc = ai.organizationData.description ?? org.Description;
                    if (!WorkspaceAdminService.TryUpdateOrganization(db, uid, org.Id, name, desc, out var err))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = err, IsUser = false });
                        });
                        return true;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nОрганизация обновлена.", IsUser = false });
                    });
                    return true;
                }

                case "delete_organization":
                {
                    var org = db.Organizations
                        .Include(o => o.Members)
                        .FirstOrDefault(o => o.Members.Any(m => m.Id == uid));

                    if (org == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = "Нет организации для удаления.", IsUser = false });
                        });
                        return true;
                    }

                    MessageBoxResult confirm = MessageBoxResult.No;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        confirm = MessageBox.Show(
                            $"{ai.message}\n\nУдалить организацию «{org.Name}» без отката?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                    });

                    if (confirm != MessageBoxResult.Yes)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = "Удаление организации отменено.", IsUser = false });
                        });
                        return true;
                    }

                    if (!WorkspaceAdminService.TryDeleteOrganization(db, uid, org.Id, out var err))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = err, IsUser = false });
                        });
                        return true;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = "Организация удалена.", IsUser = false });
                        _viewModel.LoadTeams();
                        _viewModel.LoadTasks();
                    });
                    return true;
                }

                case "create_team" when ai.teamData != null:
                {
                    var org = db.Organizations
                        .Include(o => o.Members)
                        .FirstOrDefault(o => o.Members.Any(m => m.Id == uid));

                    if (org == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = "Сначала создайте организацию в приложении.", IsUser = false });
                        });
                        return true;
                    }

                    if (!WorkspaceAdminService.TryCreateTeam(
                            db,
                            uid,
                            org.Id,
                            ai.teamData.name ?? "",
                            ai.teamData.description,
                            out _,
                            out var err))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = err, IsUser = false });
                        });
                        return true;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nКоманда создана.", IsUser = false });
                        _viewModel.LoadTeams();
                        _viewModel.LoadTasks();
                    });
                    return true;
                }

                case "update_team" when ai.teamId.HasValue && ai.teamData != null:
                {
                    if (!WorkspaceAdminService.TryUpdateTeam(
                            db,
                            uid,
                            ai.teamId.Value,
                            ai.teamData.name ?? "",
                            ai.teamData.description,
                            out var err))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = err, IsUser = false });
                        });
                        return true;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = $"{ai.message}\n\nКоманда обновлена.", IsUser = false });
                        _viewModel.LoadTeams();
                        _viewModel.LoadTasks();
                    });
                    return true;
                }

                case "delete_team" when ai.teamId.HasValue:
                {
                    MessageBoxResult confirm = MessageBoxResult.No;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        confirm = MessageBox.Show(
                            $"{ai.message}\n\nУдалить эту команду?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                    });

                    if (confirm != MessageBoxResult.Yes)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = "Удаление команды отменено.", IsUser = false });
                        });
                        return true;
                    }

                    if (!WorkspaceAdminService.TryDeleteTeam(db, uid, ai.teamId.Value, out var err))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new ChatMessage { Text = err, IsUser = false });
                        });
                        return true;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatMessage { Text = "Команда удалена.", IsUser = false });
                        _viewModel.LoadTeams();
                        _viewModel.LoadTasks();
                    });
                    return true;
                }

                default:
                    return false;
            }
        }

        private TaskViewModel? FindTaskById(int id)
        {
            foreach (var task in _viewModel.TodoTasks)
            {
                if (task.Id == id)
                {
                    return task;
                }
            }

            foreach (var task in _viewModel.InProgressTasks)
            {
                if (task.Id == id)
                {
                    return task;
                }
            }

            foreach (var task in _viewModel.DoneTasks)
            {
                if (task.Id == id)
                {
                    return task;
                }
            }

            return null;
        }

        private void ScrollToBottom()
        {
            Dispatcher.Invoke(() =>
            {
                ChatScrollViewer.ScrollToBottom();
            });
        }
    }

    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public bool IsLoading { get; set; }
        public string BackgroundColor => IsUser ? "#264F78" : "#333333";

        public string ForegroundColor => IsUser ? "#FFFFFF" : "#CCCCCC";
        public HorizontalAlignment Alignment => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    public class AIChatResponse
    {
        public string action { get; set; } = "none";
        public int? taskId { get; set; }
        public int? teamId { get; set; }
        public TaskData? taskData { get; set; }
        public OrgAiData? organizationData { get; set; }
        public TeamAiData? teamData { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class OrgAiData
    {
        public string? name { get; set; }
        public string? description { get; set; }
    }

    public class TeamAiData
    {
        public string? name { get; set; }
        public string? description { get; set; }
    }

    public class TaskData
    {
        public string? title { get; set; }
        public string? description { get; set; }
        public string? plannedStartAt { get; set; }
        public string? plannedEndAt { get; set; }
        public string? priority { get; set; }
    }
}
