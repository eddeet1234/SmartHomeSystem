using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

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
builder.Services.AddScoped<GoogleTasksService>();
builder.Services.AddSingleton<HomeStateService>();
builder.Services.AddHostedService<TaskAnnouncementWorker>();
builder.Services.AddScoped<GoogleCalendarService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<TextToSpeechService>();

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

// Read Google auth config
var googleAuthSection = builder.Configuration.GetSection("Authentication:Google");

Console.WriteLine($"ASP.NET Core Environment: {builder.Environment.EnvironmentName}");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = googleAuthSection["ClientId"];
    options.ClientSecret = googleAuthSection["ClientSecret"];

    // Ask for access to Google Tasks
    options.Scope.Add("https://www.googleapis.com/auth/tasks");

    // Ask for access to Google Calander
    options.Scope.Add("https://www.googleapis.com/auth/calendar.readonly");

    // Save access token for later use
    options.SaveTokens = true;
    
    // Optional: set your callback path
    options.CallbackPath = "/signin-google"; // This must match your redirect URI

 
    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        var redirectUri = context.RedirectUri;
        redirectUri += "&access_type=offline&prompt=consent";
        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    };
});
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
