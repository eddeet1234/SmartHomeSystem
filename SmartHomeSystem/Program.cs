using Microsoft.EntityFrameworkCore;
using SmartHomeSystem.Data;
using SmartHomeSystem.Services;

var builder = WebApplication.CreateBuilder(args);

//only load secrets when developing
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<EspLightService>();
builder.Services.AddHostedService<LightScheduleWorker>();
builder.Services.AddScoped<CeilingLightService>();
builder.Services.AddScoped<AlarmService>();
builder.Services.AddHostedService<AlarmWorker>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HomeStateService>();

// Add temperature monitoring services
builder.Services.AddHttpClient<TemperatureService>();
builder.Services.AddScoped<TemperatureService>();
builder.Services.AddHostedService<TemperatureWorker>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000); // HTTP

        options.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps("/home/eddeet2001/certs/dev-cert.pfx", "Eggethor@#12345");
        });
    });
}

Console.WriteLine($"ASP.NET Core Environment: {builder.Environment.EnvironmentName}");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
