using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanTasksController : ControllerBase
    {
        private readonly KanbanTasksService _service;

        public KanbanTasksController(KanbanTasksService service)
        {
            _service = service;
        }

        [HttpGet("user/{id}")]
        public IActionResult TaskByUserId(int id)
        {
            return Ok(_service.GetKanbanByUser(id));
        }

        [HttpGet("team/{id}")]
        public IActionResult TaskByTeamId(int id)
        {
            return Ok(_service.GetKanbanByTeam(id));
        }

        [HttpPost]
        public IActionResult AddTask(CanbanDto dto)
        {
            return Ok(_service.AddKanban(dto));
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTask(int id, CanbanDto dto)
        {
            bool update = _service.UpdateTask(id, dto);

            if (update == false) return NotFound("Task konnte nicht gefunden werden!");
            return Ok(new { message = "Task erfolgreich geändert" });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            bool delete = _service.DeleteTask(id);
            if (delete == false) return NotFound("Task konnte nicht gefunden werden!");

            return Ok(new { message = "Task erfolgreich gelöscht" });
        }


    }
}
