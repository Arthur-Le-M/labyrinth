using LabyrinthApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container following Dependency Injection principle
builder.Services.AddSingleton<CrawlerService>();
builder.Services.AddSingleton<ICrawlerService>(sp => sp.GetRequiredService<CrawlerService>());
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<ILabyrinthService, LabyrinthService>();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI support
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
