using Microsoft.Extensions.Caching.Distributed;
using StartupApi.Data;
using StartupApi.Models;
using StartupApi.Services;
using Microsoft.EntityFrameworkCore;

namespace StartupApi.Repositories;

public class CachedUserRepository : IUserRepository
{
    private readonly UserRepository _decorated;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserRepository> _logger;

    public CachedUserRepository(ApplicationDbContext context, ICacheService cacheService, ILogger<CachedUserRepository> logger)
    {
        _decorated = new UserRepository(context);
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        string key = $"user_{id}";

        var cachedUser = await _cacheService.GetAsync<User>(key);
        if (cachedUser != null)
        {
            _logger.LogInformation($"User {id} retrieved from cache");
            return cachedUser;
        }

        var user = await _decorated.GetByIdAsync(id);
        if (user != null)
        {
            await _cacheService.SetAsync(key, user, TimeSpan.FromMinutes(30));
            _logger.LogInformation($"User {id} cached for 30 minutes");
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // Don't cache by email for security reasons
        return await _decorated.GetByEmailAsync(email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        string key = "users_all";

        var cachedUsers = await _cacheService.GetAsync<List<User>>(key);
        if (cachedUsers != null)
        {
            _logger.LogInformation("All users retrieved from cache");
            return cachedUsers;
        }

        var users = await _decorated.GetAllAsync();
        var userList = users.ToList();

        await _cacheService.SetAsync(key, userList, TimeSpan.FromMinutes(15));
        _logger.LogInformation("All users cached for 15 minutes");

        return userList;
    }

    public async Task<User> AddAsync(User user)
    {
        var result = await _decorated.AddAsync(user);

        // Invalidate cache
        await _cacheService.RemoveAsync("users_all");
        _logger.LogInformation("Users cache invalidated after adding new user");

        return result;
    }

    public async Task<User> UpdateAsync(User user)
    {
        var result = await _decorated.UpdateAsync(user);

        // Invalidate relevant cache entries
        await _cacheService.RemoveAsync($"user_{user.Id}");
        await _cacheService.RemoveAsync("users_all");
        _logger.LogInformation($"User cache invalidated for user {user.Id}");

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await _decorated.DeleteAsync(id);

        if (result)
        {
            // Invalidate cache
            await _cacheService.RemoveAsync($"user_{id}");
            await _cacheService.RemoveAsync("users_all");
            _logger.LogInformation($"User cache invalidated for deleted user {id}");
        }

        return result;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _decorated.EmailExistsAsync(email);
    }
}