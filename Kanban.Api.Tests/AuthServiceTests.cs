using Kanban.Api.Data;
using Kanban.Api.Hubs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kanban.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Register_RejectsDuplicateUsername()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new DataContext(options);
        context.Database.EnsureCreated();

        var service = new AuthService(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:SecretKey"] = "ThisIsAnExampleSecretKeyThatIsLongEnoughForHmac512_1234567890"
            }).Build(),
            context,
            new TestHubContext());

        Assert.True(service.Register("alice", "Password123!"));
        Assert.False(service.Register("alice", "Password123!"));
    }

    private sealed class TestHubContext : IHubContext<KanbanHub>
    {
        public IHubClients Clients => new TestClients();
        public IGroupManager Groups => new TestGroups();

        private sealed class TestClients : IHubClients
        {
            public IClientProxy All => throw new NotSupportedException();
            public IClientProxy AllExcept(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
            public IClientProxy Client(string connectionId) => throw new NotSupportedException();
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
            public IClientProxy Group(string groupName) => throw new NotSupportedException();
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
            public IClientProxy Others => throw new NotSupportedException();
            public IClientProxy OthersInGroup(string groupName) => throw new NotSupportedException();
            public IClientProxy User(string userId) => throw new NotSupportedException();
            public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        }

        private sealed class TestGroups : IGroupManager
        {
            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
