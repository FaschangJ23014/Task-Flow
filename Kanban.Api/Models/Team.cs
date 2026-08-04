using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Models;

public class Team
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string JoinPasswordHash { get; set; } = string.Empty;
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<Canban> Tasks { get; set; } = new List<Canban>();

}
