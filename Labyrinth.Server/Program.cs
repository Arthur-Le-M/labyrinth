using Labyrinth.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container following Dependency Injection principle
builder.Services.AddSingleton<CrawlerService>();
builder.Services.AddSingleton<ICrawlerService>(sp => sp.GetRequiredService<CrawlerService>());
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<ILabyrinthService, LabyrinthService>();
builder.Services.AddSingleton<IMovementService, MovementService>();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI/Swagger support
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok());

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
