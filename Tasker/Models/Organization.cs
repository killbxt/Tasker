using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<User> Members { get; set; } = new List<User>();
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}