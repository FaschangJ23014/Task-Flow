using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controller;

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

    [HttpPost("login")]
    public IActionResult Login([FromBody]TeamDto dto)
    {
        var token = teamService.JoinTeam(dto.Name, dto.Password);
        if (token == null) return BadRequest(new { message = "Falscher Teamname oder Passwort" });

        return Ok(new { token = token });
    }

    [HttpGet("{id}")]
    public IActionResult GetTeamById(int id)
    {
        var team = teamService.getTeamById(id);
        if (team == null) return BadRequest(new { message = "Team not found" });

        team.JoinPasswordHash = string.Empty;
        return Ok(team);
    }
}
