using Kanban.Api.Data;
using Kanban.Api.DTOs;
using Kanban.Api.Models;
using Kanban.Api.Hubs; 
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
        return _data.KanbanTasks
            .Where(x => x.UserId == id && x.TeamId == null)
            .AsNoTracking()
            .ToList();
    }

    public List<Canban> GetKanbanByTeam(int userId, int teamId)
    {
        var isTeamMember = _data.TeamMembers
            .Any(tm => tm.TeamId == teamId && tm.UserId == userId);

        if (!isTeamMember)
        {
            return new List<Canban>();
        }

        return _data.KanbanTasks
            .Where(x => x.TeamId == teamId)
            .AsNoTracking()
            .ToList();
    }

    public async Task<bool> AddKanban(CanbanDto dto, int userId)
    {
        int? resolvedTeamId = (dto.TeamId == 0) ? null : dto.TeamId;

        if (resolvedTeamId.HasValue &&
            !await _data.TeamMembers.AnyAsync(tm => tm.TeamId == resolvedTeamId.Value && tm.UserId == userId))
        {
            return false;
        }

        Canban kanban = new Canban
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            TeamId = resolvedTeamId,
            UserId = userId
        };

        _data.KanbanTasks.Add(kanban);
        await _data.SaveChangesAsync();

        if (resolvedTeamId.HasValue)
        {
            await _hubContext.Clients.Group("Team_" + resolvedTeamId.Value)
                .SendAsync("ReceiveTaskUpdate", "Neuer Team-Task!");
        }

        return true;
    }

    public async Task<bool> UpdateTask(int id, CanbanDto dto, int userId)
    {
        var task = await _data.KanbanTasks.FirstOrDefaultAsync(x => x.Id == id);
        if (task == null) return false;

        bool isOwner = task.UserId == userId;
        bool isTeamMember = task.TeamId.HasValue &&
                            await _data.TeamMembers.AnyAsync(tm => tm.TeamId == task.TeamId.Value && tm.UserId == userId);

        if (!isOwner && !isTeamMember) return false;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;

        await _data.SaveChangesAsync();

        if (task.TeamId.HasValue)
        {
            await _hubContext.Clients.Group("Team_" + task.TeamId.Value)
                .SendAsync("ReceiveTaskUpdate", "Task aktualisiert!");
        }

        return true;
    }

    public async Task<bool> DeleteTask(int id, int userId)
    {
        var task = await _data.KanbanTasks.FirstOrDefaultAsync(x => x.Id == id);
        if (task == null) return false;

        bool isOwner = task.UserId == userId;
        bool isTeamMember = task.TeamId.HasValue &&
                            await _data.TeamMembers.AnyAsync(tm => tm.TeamId == task.TeamId.Value && tm.UserId == userId);

        if (!isOwner && !isTeamMember) return false;

        int? teamId = task.TeamId;

        _data.KanbanTasks.Remove(task);
        await _data.SaveChangesAsync();

        if (teamId.HasValue)
        {
            await _hubContext.Clients.Group("Team_" + teamId.Value)
                .SendAsync("ReceiveTaskUpdate", "Task gelöscht!");
        }
        return true;
    }
}