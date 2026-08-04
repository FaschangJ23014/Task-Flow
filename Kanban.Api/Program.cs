using Microsoft.EntityFrameworkCore;
using Kanban.Api.Data; // Passe den Namespace an deinen Ordner an!

var builder = WebApplication.CreateBuilder(args);

// 1. Datenbank-Kontext (PostgreSQL) hinzufügen
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite("Data Source=kanban.db"));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Automatische Migration beim Start (optional, aber praktisch)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate();
}

app.Run();