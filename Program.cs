using StartupApi.Configuration;
using StartupApi.Data;
using StartupApi.Middlewares;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Reflection;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Configuration
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
var connectionString = databaseProvider == "SqlServer"
    ? builder.Configuration.GetConnectionString("SqlServerConnection")
    : builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found.");
}

Console.WriteLine($"Using Database Provider: {databaseProvider}");

// Configure DbContext based on provider
if (databaseProvider == "SqlServer")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Redis Configuration
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
Console.WriteLine($"Using Redis Connection: {redisConnectionString}");

try
{
    // Add Redis Connection
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    // Add Distributed Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "StartupApi_";
    });

    Console.WriteLine("Redis services configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Redis configuration failed: {ex.Message}");
    // Continue without Redis for development
}

// Application Services
builder.Services.AddApplicationServices(builder.Configuration);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new ArgumentNullException("JWT SecretKey is not configured");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(secretKey))
    };
});

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// Health Checks
var healthChecks = builder.Services.AddHealthChecks();

if (databaseProvider == "SqlServer")
{
    healthChecks.AddSqlServer(connectionString, name: "sqlserver");
}
else
{
    healthChecks.AddNpgSql(connectionString, name: "postgres");
}

// Add Redis health check if configured
try
{
    healthChecks.AddRedis(redisConnectionString, name: "redis");
    Console.WriteLine("Redis health check configured");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Redis health check not configured: {ex.Message}");
}

healthChecks.AddDbContextCheck<ApplicationDbContext>(name: "database");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Startup API v1");
        options.RoutePrefix = "swagger";
    });
}

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();

// Rate Limiter must be after UseHttpsRedirection and before other middlewares
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Initialize Database
await InitializeDatabaseAsync(app);

app.Run();

async Task InitializeDatabaseAsync(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var databaseProvider = configuration["DatabaseProvider"] ?? "PostgreSQL";

        Console.WriteLine($"Initializing database for provider: {databaseProvider}");

        // Wait for database to be available
        var retries = 12;
        while (retries > 0)
        {
            try
            {
                Console.WriteLine($"Attempting to connect to {databaseProvider} database... (Retries left: {retries})");
                await context.Database.CanConnectAsync();
                Console.WriteLine("Database connection successful!");
                break;
            }
            catch (Exception ex)
            {
                retries--;
                Console.WriteLine($"Database not ready yet. Retries left: {retries}. Error: {ex.Message}");
                await Task.Delay(5000);
            }
        }

        if (retries == 0)
        {
            throw new TimeoutException("Database connection timeout after 60 seconds");
        }

        // Create database if not exists and apply migrations
        Console.WriteLine("Creating database and applying migrations...");
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Database ready!");

        // Test Redis connection
        try
        {
            var redis = services.GetService<IConnectionMultiplexer>();
            if (redis != null)
            {
                var db = redis.GetDatabase();
                await db.PingAsync();
                Console.WriteLine("Redis connection successful!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Redis connection failed: {ex.Message}");
        }

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        // Don't throw, continue without database for development
    }
}