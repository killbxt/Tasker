using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? OrganizationId { get; set; }
        public virtual Organization? Organization { get; set; }

        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<Task> AssignedTasks { get; set; } = new List<Models.Task>();
        public virtual ICollection<Task> CreatedTasks { get; set; } = new List<Models.Task>();
    }
}