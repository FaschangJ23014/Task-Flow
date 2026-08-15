using Kanban.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.DTOs
{
    public class CanbanDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Todo";
        public int? TeamId { get; set; }
    }
}
