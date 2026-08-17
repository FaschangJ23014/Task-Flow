using Microsoft.AspNetCore.SignalR;
namespace Kanban.Api.Hubs;

public class KanbanHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        //hole die TeamId aus dem Token des Users
        var teamId = Context.User?.FindFirst("TeamId")?.Value;
        if (!string.IsNullOrEmpty(teamId) && teamId != "0")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Team_" + teamId);
            Console.WriteLine($"Client {Context.ConnectionId} wurde Gruppe Team_{teamId} hinzugefügt.");
        }
        await base.OnConnectedAsync();
    }
}
