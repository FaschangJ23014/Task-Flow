using Kanban.Api.Data;
using Kanban.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Kanban.Api.Services;

public class AuthService
{
    private readonly PasswordHasher<User> _hasher = new();
    private readonly IConfiguration _config;
    private readonly DataContext _data;

    public AuthService(IConfiguration config, DataContext data)
    {
        _config = config;
        _data = data;
    }

    public string CreateToken(User user)
    {
        // 1. Claims: Das sind die Informationen, die im Token stecken sollen
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        // 2. Secret Key: Der wird aus der Konfiguration gelesen(Render Environment)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["JWT:SecretKey"] ?? "DiesIstEinStandardKeyDerNurZumTestenDient"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // 3. Token zusammenbauen
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(1), // Token ist 1 Tag gültig
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword) == PasswordVerificationResult.Success;

    public bool Register(string username, string password)
    {
        if(_data.Users.Any(x => x.Username == username)) return false;

        var user = new User
        {
            Username = username
        };

        user.PasswordHash = HashPassword(user, password);

        _data.Users.Add(user);
        _data.SaveChanges();
        return true;
    }

    public string? Login(string username, string password)
    {
        var user = _data.Users.FirstOrDefault(x => x.Username == username);
        if (user == null) return null;

        bool isPasswordValid = VerifyPassword(user, user.PasswordHash, password);
        if(isPasswordValid == false) return null;

        return CreateToken(user);
    }
}
