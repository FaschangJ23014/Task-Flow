using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<Canban> KanbanTask { get; set; } = new List<Canban>();




}
