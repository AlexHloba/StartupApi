using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace StartupApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(HealthCheckService healthCheckService, IConnectionMultiplexer redis = null)
    {
        _healthCheckService = healthCheckService;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            results = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration
            }),
            redisStatus = await GetRedisStatus()
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    private async Task<string> GetRedisStatus()
    {
        try
        {
            if (_redis == null) return "Not configured";

            var db = _redis.GetDatabase();
            await db.PingAsync();
            return "Connected";
        }
        catch (Exception ex)
        {
            return $"Disconnected: {ex.Message}";
        }
    }
}