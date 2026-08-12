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

    public KanbanTasksController(KanbanTasksService service)
    {
        _service = service;
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
        return Ok(_service.GetKanbanByTeam(id));
    }

    [HttpPost]
    public IActionResult AddTask(CanbanDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Keine gültige User-ID im Token gefunden.");
        }

        int userId = int.Parse(userIdString);
        bool success = _service.AddKanban(dto, userId);
        return Ok(success);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateTask(int id, CanbanDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool update = _service.UpdateTask(id, dto, userId);

        if (update == false) return NotFound("Task konnte nicht gefunden werden oder gehört dir nicht!");
        return Ok(new { message = "Task erfolgreich geändert" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteTask(int id)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool delete = _service.DeleteTask(id, userId); 
        if (delete == false) return NotFound("Task konnte nicht gefunden werden oder gehört dir nicht!");

        return Ok(new { message = "Task erfolgreich gelöscht" });
    }


}
