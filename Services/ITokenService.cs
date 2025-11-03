using StartupApi.Models;

namespace StartupApi.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
    bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}