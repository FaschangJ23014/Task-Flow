using Kanban.Api.Data;
using Kanban.Api.DTOs;
using Kanban.Api.Models;

namespace Kanban.Api.Services;

public class KanbanTasksService
{

    private readonly DataContext _data;

    public KanbanTasksService(DataContext data)
    {
        _data = data;
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
        Canban kanban = new Canban
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            TeamId = dto.TeamId,
            UserId = userId
        };

        _data.KanbanTasks.Add(kanban);
        _data.SaveChanges();
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
        return true;
    }

    public bool DeleteTask(int id, int userId)
    {
        var task = _data.KanbanTasks.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (task == null) return false;

        _data.KanbanTasks.Remove(task);
        _data.SaveChanges();
        return true;
    }



}
