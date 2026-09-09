using System.Collections.Concurrent;

namespace Kanban.Api.Services;

public class AuthRateLimitService
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _attempts = new();
    private readonly TimeSpan _window = TimeSpan.FromMinutes(5);
    private readonly int _maxAttempts = 5;

    public bool TryConsume(HttpContext httpContext, string? username)
    {
        var normalizedUsername = username?.Trim();
        var ipAddress = ExtractClientIp(httpContext);
        var key = string.IsNullOrWhiteSpace(normalizedUsername)
            ? $"ip:{ipAddress}"
            : $"user:{normalizedUsername.ToLowerInvariant()}|ip:{ipAddress}";

        var now = DateTimeOffset.UtcNow;
        var attempts = _attempts.GetOrAdd(key, _ => new List<DateTimeOffset>());

        lock (attempts)
        {
            attempts.RemoveAll(entry => entry < now - _window);

            if (attempts.Count >= _maxAttempts)
            {
                return false;
            }

            attempts.Add(now);
            return true;
        }
    }

    private static string ExtractClientIp(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
