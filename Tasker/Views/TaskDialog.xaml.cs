using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class TaskDialog : Window
    {
        public Models.Task? ResultTask { get; private set; }
        private readonly Models.Task? _editingTask;
        private readonly User _currentUser;

        public TaskDialog(
            User currentUser,
            ObservableCollection<TeamFilterItem> workspaceOptions,
            Models.Task? task = null,
            Team? selectedTeam = null)
        {
            InitializeComponent();
            _editingTask = task;
            _currentUser = currentUser;

            TeamComboBox.ItemsSource = workspaceOptions;
            TeamComboBox.SelectionChanged += (_, _) => RefreshAssignees();

            if (task != null)
            {
                TitleTextBox.Text = task.Title;
                DescriptionTextBox.Text = task.Description;
                StartDatePicker.SelectedDate = task.PlannedStartAt?.Date;
                StartTimeTextBox.Text = task.PlannedStartAt?.ToString("HH:mm") ?? "";
                EndDatePicker.SelectedDate = task.PlannedEndAt.Date;
                EndTimeTextBox.Text = task.PlannedEndAt.ToString("HH:mm");

                PriorityComboBox.SelectedIndex = task.Priority == TaskPriority.Urgent ? 1 : 0;
                StatusComboBox.SelectedIndex = (int)task.Status;

                TeamComboBox.SelectedItem = workspaceOptions.FirstOrDefault(o =>
                    task.TeamId == null ? o.Team == null : o.Team?.Id == task.TeamId);

                RefreshAssignees();
                if (task.AssignedTo != null)
                {
                    AssigneeComboBox.SelectedItem = task.AssignedTo;
                }
            }
            else
            {
                if (selectedTeam != null)
                {
                    TeamComboBox.SelectedItem = workspaceOptions.FirstOrDefault(o => o.Team?.Id == selectedTeam.Id);
                }
                else
                {
                    TeamComboBox.SelectedItem = workspaceOptions.FirstOrDefault(o => o.Team == null)
                        ?? workspaceOptions.FirstOrDefault();
                }

                StartDatePicker.SelectedDate = DateTime.Now.Date;
                StartTimeTextBox.Text = "09:00";
                EndDatePicker.SelectedDate = DateTime.Now.AddDays(7).Date;
                EndTimeTextBox.Text = "18:00";
                StatusComboBox.SelectedIndex = 0;
                PriorityComboBox.SelectedIndex = 0;
                RefreshAssignees();
            }
        }

        private void RefreshAssignees()
        {
            AssigneeComboBox.ItemsSource = null;
            AssigneeComboBox.SelectedItem = null;

            if (TeamComboBox.SelectedItem is not TeamFilterItem pick || pick.Team == null)
            {
                AssigneeComboBox.IsEnabled = false;
                return;
            }

            var team = pick.Team;

            var isOwner = team.OwnerId == _currentUser.Id;
            AssigneeComboBox.IsEnabled = isOwner;

            var members = team.Members?.ToList() ?? new List<User>();
            AssigneeComboBox.ItemsSource = members;

            if (isOwner)
            {
                AssigneeComboBox.SelectedItem = members.FirstOrDefault(m => m.Id == _currentUser.Id);
            }
        }

        private static bool TryParseTime(string input, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(input.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out time)
                || TimeSpan.TryParseExact(input.Trim(), "h\\:mm", CultureInfo.InvariantCulture, out time);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название задачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите окончание (дату)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseTime(EndTimeTextBox.Text, out var endTime))
            {
                MessageBox.Show("Неверный формат времени окончания. Пример: 18:00", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? plannedStartAt = null;
            if (StartDatePicker.SelectedDate != null && !string.IsNullOrWhiteSpace(StartTimeTextBox.Text))
            {
                if (!TryParseTime(StartTimeTextBox.Text, out var startTime))
                {
                    MessageBox.Show("Неверный формат времени начала. Пример: 09:00", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                plannedStartAt = StartDatePicker.SelectedDate.Value.Date.Add(startTime);
            }

            var plannedEndAt = EndDatePicker.SelectedDate.Value.Date.Add(endTime);
            if (plannedStartAt != null && plannedStartAt.Value > plannedEndAt)
            {
                MessageBox.Show("Начало не может быть позже окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultTask = _editingTask ?? new Models.Task();
            ResultTask.Title = TitleTextBox.Text;
            ResultTask.Description = DescriptionTextBox.Text;
            ResultTask.PlannedStartAt = plannedStartAt;
            ResultTask.PlannedEndAt = plannedEndAt;
            ResultTask.Priority = PriorityComboBox.SelectedIndex == 1 ? TaskPriority.Urgent : TaskPriority.Normal;
            ResultTask.Status = (TaskState)StatusComboBox.SelectedIndex;

            if (TeamComboBox.SelectedItem is TeamFilterItem area && area.Team != null)
            {
                ResultTask.TeamId = area.Team.Id;
            }
            else
            {
                ResultTask.TeamId = null;
            }

            if (AssigneeComboBox.IsEnabled && AssigneeComboBox.SelectedItem is User assignee)
            {
                ResultTask.AssignedToId = assignee.Id;
            }
            else
            {
                ResultTask.AssignedToId ??= _currentUser.Id;
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
