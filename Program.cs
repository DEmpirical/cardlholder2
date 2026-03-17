using GallagherCardholders.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<GallagherClient>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
