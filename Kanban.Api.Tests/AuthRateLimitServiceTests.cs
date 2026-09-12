using System.Net;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kanban.Api.Tests;

public class AuthRateLimitServiceTests
{
    [Fact]
    public void TryConsume_AllowsFiveAttemptsWithinWindow()
    {
        var service = new AuthRateLimitService();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        for (var i = 0; i < 5; i++)
        {
            Assert.True(service.TryConsume(context, "alice"));
        }

        Assert.False(service.TryConsume(context, "alice"));
    }
}
