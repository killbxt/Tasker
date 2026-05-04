using TaskManager.Models;

namespace TaskManager.ViewModels
{
    public class TeamFilterItem
    {
        public string Name { get; set; } = "";
        public Team? Team { get; set; }

        public override string ToString() => Name;
    }
}

