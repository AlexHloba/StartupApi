using StartupApi.DTOs;
using StartupApi.Models;

namespace StartupApi.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(CreateUserDto createUserDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<bool> VerifyPasswordAsync(User user, string password);
}
