using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    [Obsolete("Invites by token/confirmation are removed. This entity will be deleted after migration cleanup.")]
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
    }
}