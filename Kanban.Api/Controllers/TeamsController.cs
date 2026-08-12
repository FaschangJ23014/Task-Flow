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

    public TeamsController(TeamService _teamService)
    {
        teamService = _teamService;
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

        return Ok(new { message = "Erfolgreich dem Team beigetreten!" });
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
}
