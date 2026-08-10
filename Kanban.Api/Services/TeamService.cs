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

    public TeamService(IConfiguration config, DataContext data, IHubContext<KanbanHub> hubContext)
    {
        _config = config;
        _data = data;
        _hubContext = hubContext;
    }


    public string HashPassword(Team team, string password)
        => _hasher.HashPassword(team, password);

    public bool VerifyPassword(Team team, string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(team, hashedPassword, providedPassword) == PasswordVerificationResult.Success;

    public bool AddTeam(string name, string password)
    {
        if (_data.Teams.Any(x => x.Name == name)) return false;

        Team team = new Team
        {
            Name = name,
        };

        team.JoinPasswordHash = HashPassword(team, password);

        _data.Teams.Add(team);
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
}
