using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        public int OwnerId { get; set; }
        public virtual User Owner { get; set; } = null!;

        public virtual ICollection<User> Members { get; set; } = new List<User>();
        public virtual ICollection<Models.Task> Tasks { get; set; } = new List<Models.Task>();
    }
}