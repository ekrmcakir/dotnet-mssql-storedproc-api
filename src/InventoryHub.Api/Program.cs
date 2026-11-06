using InventoryHub.Api.Middlewares;
using InventoryHub.Infrastructure;
using InventoryHub.Infrastructure.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventoryHub API - MSSQL Stored Procedures",
        Version = "v1",
        Description = "Production-grade enterprise REST API built with .NET 7, Dapper, and Microsoft SQL Server Stored Procedures.\n\n" +
                      "Key Architectural Features:\n" +
                      "- Clean / N-Tier Architecture (Core, Infrastructure, API, Tests)\n" +
                      "- Dapper micro-ORM with Stored Procedures\n" +
                      "- Table-Valued Parameters (TVP) for bulk transactional operations\n" +
                      "- Atomic database transactions with Stock management\n" +
                      "- Multi-result set retrieval (QueryMultipleAsync)\n" +
                      "- Global Exception Handling & RFC 7807 compatible responses",
        Contact = new OpenApiContact
        {
            Name = "Ekrem Cakir",
            Url = new Uri("https://github.com/ekrmcakir")
        }
    });
});

// Register Infrastructure layer (Database connection & repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Automatically initialize database schema and seed data if not present
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryHub API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger at application root (http://localhost:5000/)
});

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// For integration test project accessibility
public partial class Program { }
