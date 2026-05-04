using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class AIChatDialog : Window
    {
        private readonly AIChatService _aiService;
        private readonly MainViewModel _viewModel;
        private readonly ObservableCollection<ChatMessage> _messages;

        public AIChatDialog(MainViewModel viewModel)
        {
            InitializeComponent();
            _aiService = new AIChatService();
            _viewModel = viewModel;
            _messages = new ObservableCollection<ChatMessage>();
            MessagesListBox.ItemsSource = _messages;

            _messages.Add(new ChatMessage
            {
                Text = "Привет! Я AI помощник. Я могу:\n\n• Создать новую задачу\n• Изменить существующую\n• Удалить задачу\n\nПримеры команд:\n\"Создай задачу сделать отчет до 25.12\"\n\"Удали задачу номер 3\"\n\"Поменяй приоритет задачи на срочный\"",
                IsUser = false
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                await SendMessage();
            }
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            var message = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _messages.Add(new ChatMessage { Text = message, IsUser = true });
            MessageTextBox.Clear();

            var loadingMsg = new ChatMessage { Text = "🤔 Думаю...", IsUser = false, IsLoading = true };
            _messages.Add(loadingMsg);
            await System.Threading.Tasks.Task.Delay(100);
            ScrollToBottom();

            var tasks = new System.Collections.Generic.List<Models.Task>();
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

            var response = await _aiService.SendMessageAsync(message, tasks);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _messages.Remove(loadingMsg);
            });

            try
            {
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonResponse = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var aiResponse = JsonSerializer.Deserialize<AIChatResponse>(jsonResponse);

                    if (aiResponse != null)
                    {
                        if (aiResponse.action == "create" && aiResponse.taskData != null)
                        {
                            var newTask = new Models.Task
                            {
                                Title = aiResponse.taskData.title ?? "Новая задача",
                                Description = aiResponse.taskData.description ?? "",
                                PlannedStartAt = DateTime.TryParse(aiResponse.taskData.plannedStartAt, out var startAt) ? startAt : null,
                                PlannedEndAt = DateTime.TryParse(aiResponse.taskData.plannedEndAt, out var endAt) ? endAt : DateTime.Now.AddDays(7),
                                Priority = aiResponse.taskData.priority == "Urgent" ? TaskPriority.Urgent : TaskPriority.Normal,
                                Status = TaskState.Todo,
                                CreatedAt = DateTime.Now
                            };
                            _viewModel.AddTask(newTask);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _messages.Add(new ChatMessage { Text = $"✅ {aiResponse.message}\n\nЗадача создана успешно!", IsUser = false });
                            });
                        }
                        else if (aiResponse.action == "update" && aiResponse.taskId.HasValue)
                        {
                            var taskToUpdate = FindTaskById(aiResponse.taskId.Value);
                            if (taskToUpdate != null && aiResponse.taskData != null)
                            {
                                if (!string.IsNullOrEmpty(aiResponse.taskData.title))
                                {
                                    taskToUpdate.Title = aiResponse.taskData.title;
                                }

                                if (!string.IsNullOrEmpty(aiResponse.taskData.description))
                                {
                                    taskToUpdate.Description = aiResponse.taskData.description;
                                }

                                if (aiResponse.taskData.priority != null)
                                {
                                    taskToUpdate.Priority = aiResponse.taskData.priority == "Urgent" ? TaskPriority.Urgent : TaskPriority.Normal;
                                }

                                if (!string.IsNullOrEmpty(aiResponse.taskData.plannedStartAt) && DateTime.TryParse(aiResponse.taskData.plannedStartAt, out var newStartAt))
                                {
                                    taskToUpdate.PlannedStartAt = newStartAt;
                                }

                                if (!string.IsNullOrEmpty(aiResponse.taskData.plannedEndAt) && DateTime.TryParse(aiResponse.taskData.plannedEndAt, out var newEndAt))
                                {
                                    taskToUpdate.PlannedEndAt = newEndAt;
                                }

                                _viewModel.UpdateTask(taskToUpdate);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _messages.Add(new ChatMessage { Text = $"✏️ {aiResponse.message}\n\nЗадача обновлена!", IsUser = false });
                                });
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _messages.Add(new ChatMessage { Text = $"❌ Не удалось найти задачу с ID {aiResponse.taskId}", IsUser = false });
                                });
                            }
                        }
                        else if (aiResponse.action == "delete" && aiResponse.taskId.HasValue)
                        {
                            var taskToDelete = FindTaskById(aiResponse.taskId.Value);
                            if (taskToDelete != null)
                            {
                                var title = taskToDelete.Title;
                                _viewModel.DeleteTask(taskToDelete);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _messages.Add(new ChatMessage { Text = $"🗑️ {aiResponse.message}\n\nЗадача \"{title}\" удалена!", IsUser = false });
                                });
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _messages.Add(new ChatMessage { Text = $"❌ Не удалось найти задачу с ID {aiResponse.taskId}", IsUser = false });
                                });
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _messages.Add(new ChatMessage { Text = aiResponse.message ?? "Понял! Чем еще могу помочь?", IsUser = false });
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
                        _messages.Add(new ChatMessage { Text = "Извините, не удалось получить ответ от сервера.", IsUser = false });
                    });
                }
            }
            catch (System.Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _messages.Add(new ChatMessage { Text = $"Извините, произошла ошибка: {ex.Message}\n\nПожалуйста, переформулируйте запрос.", IsUser = false });
                });
            }

            ScrollToBottom();
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
        public bool IsLoading { get; set; } = false;
        public string BackgroundColor => IsUser ? "#264F78" : "#333333";

        public string ForegroundColor => IsUser ? "#FFFFFF" : "#CCCCCC";
        public HorizontalAlignment Alignment => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    public class AIChatResponse
    {
        public string action { get; set; } = "none";
        public int? taskId { get; set; }
        public TaskData? taskData { get; set; }
        public string message { get; set; } = string.Empty;
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