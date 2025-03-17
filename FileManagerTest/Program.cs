using FileManagerApp.Helpers;
using FileManagerApp.Services;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Add services to the container.
// -------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the PathHelper as a singleton (stateless helper).
builder.Services.AddSingleton<PathHelper>();

// Register the FileSystemService implementation for file management.
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Serve static files (frontend assets) from wwwroot.
app.UseStaticFiles();

app.UseAuthorization();

// Map API controllers to endpoints.
app.MapControllers();

// Fallback to serve index.html for Single Page Application (SPA) routes.
app.MapFallbackToFile("index.html");

app.Run();
