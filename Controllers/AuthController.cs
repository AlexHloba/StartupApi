using MediatR;
using Microsoft.AspNetCore.Mvc;
using StartupApi.DTOs;
using StartupApi.Features.Auth.Commands;

namespace StartupApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var command = new LoginCommand { LoginDto = loginDto };
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
