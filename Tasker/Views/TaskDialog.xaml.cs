using System.Collections.ObjectModel;
using System.Windows;
using TaskManager.Models;

namespace TaskManager.Views
{
    public partial class TaskDialog : Window
    {
        public Models.Task? ResultTask { get; private set; }
        private readonly Models.Task? _editingTask;

        public TaskDialog(User currentUser, ObservableCollection<Team> teams, Models.Task? task = null, Team? selectedTeam = null)
        {
            InitializeComponent();
            _editingTask = task;

            TeamComboBox.ItemsSource = teams;
            TeamComboBox.SelectedItem = null;

            if (selectedTeam != null)
            {
                TeamComboBox.SelectedItem = selectedTeam;
            }
            else if (teams.Any())
            {
                TeamComboBox.SelectedItem = teams.FirstOrDefault();
            }

            if (task != null)
            {
                TitleTextBox.Text = task.Title;
                DescriptionTextBox.Text = task.Description;
                DueDatePicker.SelectedDate = task.DueDate;

                PriorityComboBox.SelectedIndex = task.Priority == TaskPriority.Urgent ? 1 : 0;
                StatusComboBox.SelectedIndex = (int)task.Status;

                if (task.Team != null)
                {
                    TeamComboBox.SelectedItem = task.Team;
                }
            }
            else
            {
                DueDatePicker.SelectedDate = DateTime.Now.AddDays(7);
                StatusComboBox.SelectedIndex = 0;
                PriorityComboBox.SelectedIndex = 0;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название задачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultTask = _editingTask ?? new Models.Task();
            ResultTask.Title = TitleTextBox.Text;
            ResultTask.Description = DescriptionTextBox.Text;
            ResultTask.DueDate = DueDatePicker.SelectedDate ?? DateTime.Now.AddDays(7);
            ResultTask.Priority = PriorityComboBox.SelectedIndex == 1 ? TaskPriority.Urgent : TaskPriority.Normal;
            ResultTask.Status = (TaskState)StatusComboBox.SelectedIndex;

            if (TeamComboBox.SelectedItem is Team selectedTeam)
            {
                ResultTask.TeamId = selectedTeam.Id;
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