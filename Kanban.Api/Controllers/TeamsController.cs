using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kanban.Api.Data;

namespace Kanban.Api.Controllers;


[Route("api/[controller]")]
[ApiController]
public class TeamsController : ControllerBase
{
    private readonly TeamService teamService;
    private readonly AuthService authService; //Für die TeamToken
    private readonly DataContext _data;
   

    public TeamsController(TeamService _teamService, AuthService _authService, DataContext data)
    {
        teamService = _teamService;
        authService = _authService;
        _data = data;
    }

    [Authorize]
    [HttpPost("register")] 
    public IActionResult Register([FromBody] TeamDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Teamname und Passwort dürfen nicht leer sein." });
        }

        var normalizedName = dto.Name.Trim();
        if (normalizedName.Length > 15 || dto.Password.Length < 8)
        {
            return BadRequest(new { message = "Teamname darf max. 15 Zeichen lang sein und das Passwort muss mindestens 8 Zeichen lang sein." });
        }

        bool register = teamService.AddTeam(normalizedName, dto.Password, userId);
        if (!register) return BadRequest(new { message = "Ein Team mit diesem Namen existiert bereits oder die Daten sind ungültig." });

        var user = authService.GetUserById(userId);
        if (user == null) return Unauthorized();
        var newToken = authService.CreateToken(user);

        return Ok(new { message = "Erfolgreich ein Team erstellt und beigetreten!", token = newToken });
    }

    [Authorize]
    [HttpPost("login")]
    public IActionResult Login([FromBody]TeamDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Keine gültige User-ID im Token gefunden.");
        }
        int userId = int.Parse(userIdString);

        bool success = teamService.JoinTeam(dto.Name, dto.Password, userId);
        if (!success)
        {
            return BadRequest(new { message = "Falscher Teamname, falsches Passwort, du bist bereits im Team oder das Team ist voll (max. 10 Mitglieder)!" });
        }

        var user = authService.GetUserById(userId);
        if (user == null) return Unauthorized();
        var newToken = authService.CreateToken(user);

        return Ok(new { message = "Erfolgreich dem Team beigetreten!", token = newToken });
    }

    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetTeamById(int id)
    {
        var team = teamService.getTeamById(id);
        if (team == null) return BadRequest(new { message = "Team not found" });

        team.JoinPasswordHash = string.Empty;
        return Ok(team);
    }

    [Authorize]
    [HttpGet("members/{teamId}")]
    public IActionResult GetTeamMembers(int teamId)
    {
        var members = _data.TeamMembers
         .Where(x => x.TeamId == teamId)
         .Select(x => new {
             id = x.User.Id,      
             username = x.User.Username, 
             isAdmin = x.IsAdmin      
         })
         .ToList();

    return Ok(members);
    }

    [Authorize]
    [HttpPost("leave")]
    public IActionResult LeaveTeam()
    {
    var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

    int userId = int.Parse(userIdString);
    
    string? newToken = teamService.LeaveTeam(userId);

    if (newToken == null) 
    {
        return BadRequest("Du bist in keinem Team oder ein Fehler ist aufgetreten.");
    }

    return Ok(new { 
        token = newToken, 
        message = "Team erfolgreich verlassen." 
    });
    }

    [Authorize]
    [HttpPost("kick/{teamId}/{targetUserId}")]
    public IActionResult KickMember(int teamId, int targetUserId)
    {
    var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
    int adminUserId = int.Parse(userIdString);

    bool success = teamService.RemoveMemberFromTeam(adminUserId, targetUserId, teamId);
    if (!success) return BadRequest(new { message = "Konnte Mitglied nicht kicken (Keine Admin-Rechte oder falsches Team?)." });

    return Ok(new { message = "Mitglied erfolgreich gekickt." });
    }
}
