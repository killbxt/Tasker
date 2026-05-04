using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public enum TaskState
    {
        Todo = 0,
        InProgress = 1,
        Done = 2
    }

    public enum TaskPriority
    {
        Normal = 0,
        Urgent = 1
    }

    public class Task
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TaskState Status { get; set; } = TaskState.Todo;

        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        public DateTime? PlannedStartAt { get; set; }

        public DateTime PlannedEndAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int CreatedById { get; set; }
        public virtual User CreatedBy { get; set; } = null!;

        public int? AssignedToId { get; set; }
        public virtual User? AssignedTo { get; set; }

        public int? TeamId { get; set; }
        public virtual Team? Team { get; set; }
    }
}