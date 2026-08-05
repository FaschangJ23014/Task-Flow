using Kanban.Api.Data;
using Kanban.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Kanban.Api.Services;

public class TeamService
{
    private readonly PasswordHasher<Team> _hasher = new();
    private readonly IConfiguration _config;
    private readonly DataContext _data;

    public TeamService(IConfiguration config, DataContext data)
    {
        _config = config;
        _data = data;
    }

    public string CreateToken(Team team)
    {
        // 1. Claims: Das sind die Informationen, die im Token stecken sollen
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, team.Id.ToString()),
            new Claim(ClaimTypes.Name, team.Name)
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

    public string HashPassword(Team team, string password)
        => _hasher.HashPassword(team, password);

    public bool VerifyPassword(Team team, string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(team, hashedPassword, providedPassword) == PasswordVerificationResult.Success;

    public bool AddTeam(string name, string password)
    {
        if (_data.Teams.Any(x => x.Name == name)) return false;

        Team team = new Team
        {
            Name = name,
        };

        team.JoinPasswordHash = HashPassword(team, password);

        _data.Teams.Add(team);
        _data.SaveChanges();
        return true;
    }

    public string? JoinTeam(string name, string password)
    {
        var team = _data.Teams.FirstOrDefault(x => x.Name == name);
        if (team == null) return null;

        bool verify = VerifyPassword(team, team.JoinPasswordHash, password);
        if (!verify) return null;

        return CreateToken(team);
    }

    public Team? getTeamById(int id)
    {
        var team = _data.Teams.FirstOrDefault(x =>x.Id == id);
        if(team == null) return null;

        return team;
    }
}
