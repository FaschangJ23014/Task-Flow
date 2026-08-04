using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Models
{
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
