using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Models
{
    public class Canban
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Todo";

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? TeamId { get; set; }
        public Team? Team { get; set; }

    }
}
