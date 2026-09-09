using Kanban.Api.Data;
using Kanban.Api.Hubs;
using Kanban.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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

    public async Task<bool> AddTeam(string name, string password, int userId)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password)) return false;

        var normalizedName = name.Trim();
        if (normalizedName.Length < 3 || normalizedName.Length > 15 || password.Length < 8) return false;
        if (_data.Teams.Any(x => x.Name.ToLower() == normalizedName.ToLower())) return false;

        Team team = new Team
        {
            Name = normalizedName,
        };

        team.JoinPasswordHash = HashPassword(team, password);

        _data.Teams.Add(team);
        await _data.SaveChangesAsync();

        TeamMember creatorMember = new TeamMember
        {
            UserId = userId,
            TeamId = team.Id,
            IsAdmin = true
        };

        _data.TeamMembers.Add(creatorMember);
        await _data.SaveChangesAsync();
        return true;
    }

    public async Task<bool> JoinTeam(string name, string password, int userId)
    {
        var normalizedName = name.Trim();
        var team = await _data.Teams.FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName.ToLower());
        if (team == null) return false;

        bool verify = VerifyPassword(team, team.JoinPasswordHash, password);
        if (!verify) return false;

        bool alreadyMember = await _data.TeamMembers.AnyAsync(tm => tm.UserId == userId && tm.TeamId == team.Id);
        if (alreadyMember) return false;

        int memberCount = await _data.TeamMembers.CountAsync(tm => tm.TeamId == team.Id);
        if (memberCount >= 10)
        {
            return false;
        }

        TeamMember member = new TeamMember
        {
            UserId = userId,
            TeamId = team.Id
        };

        _data.TeamMembers.Add(member);
        await _data.SaveChangesAsync();

        await _hubContext.Clients.Group("Team_" + team.Id).SendAsync("UserJoined", userId);

        return true;
    }

    public Team? getTeamById(int id)
    {
        var team = _data.Teams.FirstOrDefault(x =>x.Id == id);
        if(team == null) return null;

        return team;
    }

public async Task<string?> LeaveTeam(int userId, int? teamId = null)
    {
    var memberships = await _data.TeamMembers
        .Where(tm => tm.UserId == userId)
        .ToListAsync();

    if (memberships.Count == 0) return null;

    var teamMember = teamId.HasValue
        ? memberships.FirstOrDefault(tm => tm.TeamId == teamId.Value)
        : memberships.Count == 1 ? memberships[0] : null;

    if (teamMember == null) return null;

    int leavingTeamId = teamMember.TeamId;

    _data.TeamMembers.Remove(teamMember);
    await _data.SaveChangesAsync();

    await _hubContext.Clients.Group("Team_" + leavingTeamId).SendAsync("UserLeft", userId);

    var user = await _data.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return null;

    return authService.CreateToken(user);
}

public async Task<bool> RemoveMemberFromTeam(int adminUserId, int targetUserId, int teamId)
{
    var adminMembership = await _data.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == adminUserId && tm.TeamId == teamId && tm.IsAdmin);
    if (adminMembership == null) return false;

    var targetMembership = await _data.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == targetUserId && tm.TeamId == teamId);
    if (targetMembership == null) return false;

    if (adminUserId == targetUserId) return false;

    _data.TeamMembers.Remove(targetMembership);
    await _data.SaveChangesAsync();

    await _hubContext.Clients.Group("Team_" + teamId).SendAsync("YouWereKicked", targetUserId);

    return true;
}

}
