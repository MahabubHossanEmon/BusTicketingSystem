using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BusTicketingSystem.Domain.Interfaces;
using BusTicketingSystem.Infrastructure.Data;
using BusTicketingSystem.Infrastructure.Repositories;
using BusTicketingSystem.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers
builder.Services.AddControllers();

// Register ADO.NET Connection Factory & Repository Services (Pure ADO.NET Architecture)
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITicketingService, TicketingService>();

// CORS for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Initialize SQL Database Tables (ADO.NET)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        await db.InitializeDatabaseAsync();
        await AdoDbSeeder.SeedIfEmptyAsync(db);
        Console.WriteLine("Database tables initialized and synchronized via ADO.NET.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SQL Connection / Schema Note: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
