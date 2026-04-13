using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class Invitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        public int? TeamId { get; set; }
        public virtual Team? Team { get; set; }

        public int InvitedById { get; set; }
        public virtual User InvitedBy { get; set; } = null!;

        public string Token { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(7);

        public bool IsUsed { get; set; } = false;
    }
}