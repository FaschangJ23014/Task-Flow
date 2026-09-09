using Kanban.Api.Data;
using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class KanbanTasksController : ControllerBase
{
    private readonly KanbanTasksService _service;
    private readonly DataContext _data;

    public KanbanTasksController(KanbanTasksService service, DataContext data)
    {
        _service = service;
        _data = data;
    }

    [HttpGet("user")]
    public IActionResult GetMyTasks()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Keine gültige User-ID im Token gefunden.");
        }

        int userId = int.Parse(userIdString);
        return Ok(_service.GetKanbanByUser(userId));
    }

    [HttpGet("team/{id}")]
    public IActionResult TaskByTeamId(int id)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        int userId = int.Parse(userIdString);

        var isMember = _data.TeamMembers.Any(tm => tm.TeamId == id && tm.UserId == userId);
        if (!isMember)
        {
            return Forbid();
        }

        return Ok(_service.GetKanbanByTeam(userId, id));
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(CanbanDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Keine gültige User-ID im Token gefunden.");
        }

        int userId = int.Parse(userIdString);
        bool success = await _service.AddKanban(dto, userId);
        if (!success)
        {
            return Forbid();
        }

        return Ok(success);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, CanbanDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool update = await _service.UpdateTask(id, dto, userId);

        if (update == false) return NotFound("Task konnte nicht gefunden werden oder gehört dir nicht!");
        return Ok(new { message = "Task erfolgreich geändert" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool delete = await _service.DeleteTask(id, userId);
        if (delete == false) return NotFound("Task konnte nicht gefunden werden oder gehört dir nicht!");

        return Ok(new { message = "Task erfolgreich gelöscht" });
    }


}
