namespace StartupApi.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, List<DateTime>> _requestLog = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1); // 1-minute window

        // Clean old entries
        CleanOldEntries(now);

        // Check rate limit
        if (!_requestLog.ContainsKey(clientIp))
        {
            _requestLog[clientIp] = new List<DateTime>();
        }

        var clientRequests = _requestLog[clientIp];
        clientRequests.Add(now);

        // Limit: 100 requests per minute per IP
        if (clientRequests.Count(r => r > windowStart) > 100)
        {
            _logger.LogWarning($"Rate limit exceeded for IP: {clientIp}");
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private void CleanOldEntries(DateTime now)
    {
        var cutoff = now.AddMinutes(-5); // Keep last 5 minutes for cleanup
        var keysToRemove = new List<string>();

        foreach (var entry in _requestLog)
        {
            entry.Value.RemoveAll(t => t < cutoff);
            if (entry.Value.Count == 0)
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _requestLog.Remove(key);
        }
    }
}