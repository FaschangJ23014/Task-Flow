using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controllers;


[Route("api/[controller]")]
[ApiController]
public class TeamsController : ControllerBase
{
    private readonly TeamService teamService;
    private readonly AuthService authService; //Für die TeamToken
   

    public TeamsController(TeamService _teamService, AuthService _authService)
    {
        teamService = _teamService;
        authService = _authService;
    }

    [HttpPost("register")] 
    public IActionResult Register([FromBody] TeamDto dto)
    {
        bool register = teamService.AddTeam(dto.Name, dto.Password);
        if (!register) return BadRequest(new { message = "Ein Team mit diesem Namen existiert bereits" });

        return Ok(new { message = "Team erfolreich erstellt!" });
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
        var members = teamService.GetTeamMembers(teamId);
        
        // mappe es anonym, damit Passwörter oder Hashes auf gar keinen Fall nach außen wandern
        var result = members.Select(m => new {
            m.Id,
            m.Username
        });

        return Ok(result);
    }

    [HttpPost("leave")]
    public IActionResult LeaveTeam()
    {
    var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

    int userId = int.Parse(userIdString);
    bool success = teamService.LeaveTeam(userId);

    if (!success) return BadRequest("Du bist in keinem Team oder ein Fehler ist aufgetreten.");

    return Ok(new { message = "Team erfolgreich verlassen" });
    }
}
