using StackExchange.Redis;
using StartupApi.Data;
using StartupApi.Repositories;
using StartupApi.Services;
namespace StartupApi.Configuration;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis Configuration
        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "redis:6379";

        try
        {
            // Add Redis Connection if not already added
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            // Services
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        catch (Exception ex)
        {
            // Log error but continue without Redis
            Console.WriteLine($"Redis initialization failed: {ex.Message}");
        }

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();

        // Repositories - decorated with caching
        services.AddScoped<IUserRepository>(provider =>
        {
            var context = provider.GetRequiredService<ApplicationDbContext>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedUserRepository>>();

            return new CachedUserRepository(context, cacheService, logger);
        });

        return services;
    }
}