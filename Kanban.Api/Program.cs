using Microsoft.EntityFrameworkCore;
using Kanban.Api.Data;
using Kanban.Api.Hubs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Datenbank
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite("Data Source=kanban.db"));

// 2. Services registrieren
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<KanbanTasksService>();

// 3. SignalR
// 3. SignalR mit Ping- und Timeout-Einstellungen
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// --- CORS POLICY HINZUFÜGEN ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // Trage hier die URL deines Svelte-Frontends ein (Standard bei Vite ist meist 5173)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Wichtig: Erlaubt Credentials, setzt voraus, dass Origins explizit genannt werden (kein "*")
    });
});

// 4. JWT-Authentifizierung aktivieren 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["JWT:SecretKey"] ?? "DiesIstEinStandardKeyDerNurZurTestenDient")),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/kanbanHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS MUSS GANZ NACH OBEN
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<KanbanHub>("/kanbanHub");

// Datenbank-Migrationen beim Start automatisch anwenden
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        context.Database.Migrate();
        Console.WriteLine("Datenbank & Migrationen wurden erfolgreich angewendet.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Fehler beim Migrieren der Datenbank: " + ex.Message);
    }
}

app.Run();