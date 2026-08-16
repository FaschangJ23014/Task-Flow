using Kanban.Api.Data;
using Kanban.Api.DTOs;
using Kanban.Api.Models;
using Kanban.Api.Hubs; 
using Microsoft.AspNetCore.SignalR;

namespace Kanban.Api.Services;

public class KanbanTasksService
{

    private readonly DataContext _data;
    private readonly IHubContext<KanbanHub> _hubContext;

    public KanbanTasksService(DataContext data, IHubContext<KanbanHub> hubContext)
    {
        _data = data;
        _hubContext = hubContext;
    }
    public List<Canban> GetKanbanByUser(int id)
    {
        return _data.KanbanTasks.Where(x => x.UserId == id).ToList();
    }

    public List<Canban> GetKanbanByTeam(int id)
    {
        return _data.KanbanTasks.Where(x => x.TeamId == id).ToList();
    }

    public bool AddKanban(CanbanDto dto, int userId)
    {
        int? resolvedTeamId = (dto.TeamId == 0) ? null : dto.TeamId;

        Canban kanban = new Canban
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            TeamId = resolvedTeamId,
            UserId = userId
        };

        _data.KanbanTasks.Add(kanban);
        _data.SaveChanges();

        if (resolvedTeamId.HasValue)
        {
            _hubContext.Clients.Group("Team_" + resolvedTeamId.Value).SendAsync("ReceiveTaskUpdate", "Neuer Team-Task!");
        }

        return true;
    }


    public bool UpdateTask(int id, CanbanDto dto, int userId)
    {
        var task = _data.KanbanTasks.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (task == null) return false;


        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;

        _data.SaveChanges();

        if (task.TeamId.HasValue)
        {
            _hubContext.Clients.Group("Team_" + task.TeamId.Value).SendAsync("ReceiveTaskUpdate", "Task aktualisiert!");
        }

        return true;
    }

    public bool DeleteTask(int id, int userId)
    {
        var task = _data.KanbanTasks.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (task == null) return false;

        int? teamId = task.TeamId;

        _data.KanbanTasks.Remove(task);
        _data.SaveChanges();

        if (teamId.HasValue)
        {
            _hubContext.Clients.Group("Team_" + teamId.Value).SendAsync("ReceiveTaskUpdate", "Task gelöscht!");
        }
        return true;
    }



}
