using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Kanban.Api.Models;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<Canban> KanbanTask { get; set; } = new List<Canban>();




}
