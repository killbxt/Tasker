using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManager.ViewModels
{
    public class TaskViewModel : INotifyPropertyChanged
    {
        private readonly Models.Task _task;

        public TaskViewModel(Models.Task task)
        {
            _task = task;
        }

        public Models.Task Task => _task;

        public int Id => _task.Id;

        public string Title
        {
            get => _task.Title;
            set
            {
                _task.Title = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _task.Description;
            set
            {
                _task.Description = value;
                OnPropertyChanged();
            }
        }

        public Models.TaskState Status
        {
            get => _task.Status;
            set
            {
                _task.Status = value;
                OnPropertyChanged();
            }
        }

        public Models.TaskPriority Priority
        {
            get => _task.Priority;
            set
            {
                _task.Priority = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriorityColor));
            }
        }

        public DateTime? PlannedStartAt
        {
            get => _task.PlannedStartAt;
            set
            {
                _task.PlannedStartAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedWindowText));
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        public DateTime PlannedEndAt
        {
            get => _task.PlannedEndAt;
            set
            {
                _task.PlannedEndAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedWindowText));
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        public string PriorityColor => Priority == Models.TaskPriority.Urgent ? "#F14C4C" : "#3794FF";

        public string PlannedWindowText
        {
            get
            {
                var end = PlannedEndAt;
                if (PlannedStartAt == null)
                {
                    return end.ToString("dd.MM.yyyy HH:mm");
                }

                var start = PlannedStartAt.Value;
                if (start.Date == end.Date)
                {
                    return $"{start:dd.MM.yyyy HH:mm}–{end:HH:mm}";
                }

                return $"{start:dd.MM.yyyy HH:mm}–{end:dd.MM.yyyy HH:mm}";
            }
        }

        public bool IsOverdue => PlannedEndAt < DateTime.Now && Status != Models.TaskState.Done;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}