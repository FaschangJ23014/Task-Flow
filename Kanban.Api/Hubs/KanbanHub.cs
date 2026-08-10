using Microsoft.AspNetCore.SignalR;
namespace Kanban.Api.Hubs;

public class KanbanHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        //hole die TeamId aus dem Token des Users
        var teamId = Context.User?.FindFirst("TeamId")?.Value;
        if (teamId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Team_" + teamId);
        }
        await base.OnConnectedAsync();
    }
}
