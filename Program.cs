using StartupApi.Configuration;
using StartupApi.Data;
using StartupApi.Middlewares;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Reflection;

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

// Application Services
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

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

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        // Don't throw, continue without database for development
    }
}