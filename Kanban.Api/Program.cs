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

// 2.  Services registrieren
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<KanbanTasksService>();

// 3. SignalR
builder.Services.AddSignalR();

// --- 1. CORS POLICY HINZUFÜGEN ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSvelte", policy =>
    {
        policy.AllowAnyOrigin() // Oder spezifisch: .WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
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
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 1. CORS MUSS GANZ NACH OBEN (vor HttpsRedirection und Auth)
app.UseCors("AllowSvelte");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<KanbanHub>("/kanbanHub");

// Migrationen beim Start
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate();
}

app.Run();