using Microsoft.EntityFrameworkCore;
using Kanban.Api.Data;
using Kanban.Api.Hubs;
using Kanban.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var configuredAllowedOrigins = builder.Configuration["Frontend:AllowedOrigins"]
    ?? Environment.GetEnvironmentVariable("FRONTEND_ALLOWED_ORIGINS")
    ?? "http://localhost:5173,http://localhost:3000";

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Data Source=kanban.db";

// 1. Datenbank
builder.Services.AddDbContext<DataContext>(options =>
{
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
       connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
    {
       options.UseNpgsql(connectionString);
    }
    else
    {
       options.UseSqlite(connectionString);
    }
});

// 2. Services registrieren
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<KanbanTasksService>();
builder.Services.AddSingleton<AuthRateLimitService>();

builder.Services.AddHealthChecks();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    options.RequestBodyLogLimit = 4096;
    options.ResponseBodyLogLimit = 4096;
});

// 3. SignalR
// 3. SignalR mit Ping- und Timeout-Einstellungen
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Zu viele Anfragen. Bitte warte kurz und versuche es erneut."
        }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isAuthRoute = (path.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("/register", StringComparison.OrdinalIgnoreCase)) &&
                          (path.Contains("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("/api/teams", StringComparison.OrdinalIgnoreCase));

        if (!isAuthRoute)
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// --- CORS POLICY HINZUFÜGEN ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = configuredAllowedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtSecret = builder.Configuration["JWT:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT secret is missing. Set JWT_SECRET_KEY or JWT__SecretKey.");

// 4. JWT-Authentifizierung aktivieren 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders();
app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS MUSS GANZ NACH OBEN
app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/ready");
app.Map("/error", () => Results.Problem("Ein unerwarteter Fehler ist aufgetreten."));
app.MapControllers();
app.MapHub<KanbanHub>("/kanbanHub");

// Datenbank-Migrationen beim Start automatisch anwenden
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Datenbank & Migrationen wurden erfolgreich angewendet.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Fehler beim Migrieren der Datenbank: " + ex.Message);
    }
}

app.Run();