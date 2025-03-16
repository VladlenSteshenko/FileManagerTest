using FileManagerApp.Helpers;
using FileManagerApp.Services;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the helper as a singleton since it's stateless.
builder.Services.AddSingleton<PathHelper>();

// Register your file system service implementation
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    /*
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileManager API v1");
        // Set the Swagger UI at the root path
        c.RoutePrefix = "swagger";
    });
    */
}

app.UseHttpsRedirection();

// Serve static files from wwwroot (this is where your front-end lives)
app.UseStaticFiles();

app.UseAuthorization();

// Map API controllers.
app.MapControllers();

// Fallback to serve index.html for SPA routes.
app.MapFallbackToFile("index.html");


app.Run();
