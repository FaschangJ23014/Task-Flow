using Kanban.Api.Data;
using Kanban.Api.Hubs;
using Kanban.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Kanban.Api.Services;

public class TeamService
{
    private readonly PasswordHasher<Team> _hasher = new();
    private readonly IConfiguration _config;
    private readonly DataContext _data;
    private readonly IHubContext<KanbanHub> _hubContext;
    private readonly AuthService authService;

    public TeamService(IConfiguration config, DataContext data, IHubContext<KanbanHub> hubContext, AuthService _authService)
    {
        _config = config;
        _data = data;
        _hubContext = hubContext;
        authService = _authService;
    }


    public string HashPassword(Team team, string password)
        => _hasher.HashPassword(team, password);

    public bool VerifyPassword(Team team, string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(team, hashedPassword, providedPassword) == PasswordVerificationResult.Success;

    public bool AddTeam(string name, string password, int userId)
    {
        if (_data.Teams.Any(x => x.Name == name)) return false;

        Team team = new Team
        {
            Name = name,

        };

        team.JoinPasswordHash = HashPassword(team, password);

        _data.Teams.Add(team);
        _data.SaveChanges();

        TeamMember creatorMember = new TeamMember
        {
            UserId = userId,
            TeamId = team.Id,
            IsAdmin = true // <--- HIER WIRD ER ADMIN DER GRUPPE!
        };

        _data.TeamMembers.Add(creatorMember);
        _data.SaveChanges();
        return true;
    }

    public bool JoinTeam(string name, string password, int userId)
    {
        var team = _data.Teams.FirstOrDefault(x => x.Name == name);
        if (team == null) return false;

        bool verify = VerifyPassword(team, team.JoinPasswordHash, password);
        if (!verify) return false;

        bool alreadyMember = _data.TeamMembers.Any(tm => tm.UserId == userId && tm.TeamId == team.Id);
        if (alreadyMember) return false;

        int memberCount = _data.TeamMembers.Count(tm => tm.TeamId == team.Id);
        if (memberCount >= 10)
        {
            return false; // Team ist voll!
        }

        TeamMember member = new TeamMember
        {
            UserId = userId,
            TeamId = team.Id
        };

        _data.TeamMembers.Add(member);
        _data.SaveChanges();

        _hubContext.Clients.Group("Team_" + team.Id).SendAsync("UserJoined", userId);

        return true;
    }

    public Team? getTeamById(int id)
    {
        var team = _data.Teams.FirstOrDefault(x =>x.Id == id);
        if(team == null) return null;

        return team;
    }

    public string? LeaveTeam(int userId)
    {
    var teamMember = _data.TeamMembers.FirstOrDefault(tm => tm.UserId == userId);
    if (teamMember == null) return null;

    int teamId = teamMember.TeamId;

    _data.TeamMembers.Remove(teamMember);
    _data.SaveChanges();

    _hubContext.Clients.Group("Team_" + teamId).SendAsync("UserJoined", userId);

    var user = _data.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null) return null;

    // Ein NEUES Token generieren (jetzt ohne TeamId / TeamId = 0)
    string newToken = authService.CreateToken(user);

    return newToken;
   }

public bool RemoveMemberFromTeam(int adminUserId, int targetUserId, int teamId)
{
    var adminMembership = _data.TeamMembers.FirstOrDefault(tm => tm.UserId == adminUserId && tm.TeamId == teamId && tm.IsAdmin);
    if (adminMembership == null) return false; 

    var targetMembership = _data.TeamMembers.FirstOrDefault(tm => tm.UserId == targetUserId && tm.TeamId == teamId);
    if (targetMembership == null) return false;

    if (adminUserId == targetUserId) return false;

    _data.TeamMembers.Remove(targetMembership);
    _data.SaveChanges();

    _hubContext.Clients.Group("Team_" + teamId).SendAsync("YouWereKicked", targetUserId);

    return true;
}


}
