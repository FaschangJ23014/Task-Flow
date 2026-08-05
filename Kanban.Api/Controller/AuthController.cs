using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controller;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService authService;


    public AuthController(AuthService _authService)
    {
        authService = _authService;
    }

    [HttpPost("register")]
    public IActionResult Register(UserDto dto)
    {
        bool register = authService.Register(dto.Username, dto.Password);
        if (!register) return BadRequest("Username existiert bereits");

        return Ok("User erfolgreich registriert!");
    }

    [HttpPost("login")]
    public IActionResult Login(UserDto dto)
    {
        string? token = authService.Login(dto.Username, dto.Password);
        if (token == null) return BadRequest("Falscher Username oder Passwort.");

        return Ok(new { Token = token });
    }


}
