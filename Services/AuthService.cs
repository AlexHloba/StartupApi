using StartupApi.DTOs;
using StartupApi.Models;
using StartupApi.Repositories;
using AutoMapper;

namespace StartupApi.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AuthService(IUserRepository userRepository, ITokenService tokenService, IMapper mapper)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<User> RegisterAsync(CreateUserDto createUserDto)
    {
        if (await _userRepository.EmailExistsAsync(createUserDto.Email))
        {
            throw new ArgumentException("Email already exists");
        }

        _tokenService.CreatePasswordHash(createUserDto.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        return await _userRepository.AddAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null || !await VerifyPasswordAsync(user, loginDto.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var token = _tokenService.GenerateToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        return new AuthResponseDto
        {
            Token = token,
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    public async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        return _tokenService.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
    }
}