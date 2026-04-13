using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskManager.Models;

namespace TaskManager.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly Team _team;

        public TeamViewModel(Team team)
        {
            _team = team;
            Members = new ObservableCollection<User>(team.Members);
        }

        public Team Team => _team;

        public int Id => _team.Id;

        public string Name
        {
            get => _team.Name;
            set
            {
                _team.Name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _team.Description;
            set
            {
                _team.Description = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<User> Members { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}