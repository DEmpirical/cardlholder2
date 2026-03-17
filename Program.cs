using GallagherCardholders.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<GallagherClient>();
builder.Services.AddSingleton<ImportService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles(); // Serve wwwroot

app.MapControllers();

app.Run();
