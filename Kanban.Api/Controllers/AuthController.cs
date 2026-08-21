using Kanban.Api.DTOs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kanban.Api.Controllers;

[Route("api/[controller]")]
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

    [Authorize]
    [HttpPut("changepassword")]
    public IActionResult ChangePassword(ChangePasswordDto dto)
    {
        var newPassword = dto.NewPassword;
        var oldPassword = dto.OldPassword;
        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(oldPassword)) return BadRequest("Passwörter dürfen nicht leer sein.");
    
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool success = authService.UpdatePassword(userId, newPassword, oldPassword);
        if (!success) return BadRequest("Altes Passwort ist falsch.");
        return Ok("Passwort erfolgreich geändert.");
    }

    [Authorize]
    [HttpPut("changeusername")]
    public IActionResult ChangeUsername(ChangeUsernameDto dto)
    {
        var newUsername = dto.NewUsername;
        if (string.IsNullOrEmpty(newUsername)) return BadRequest("Neuer Username darf nicht leer sein.");
    
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        int userId = int.Parse(userIdString);

        bool success = authService.UpdateUsername(userId, newUsername);
        if (!success) return BadRequest("Username existiert bereits.");
        return Ok("Username erfolgreich geändert.");
    }


}
