using Ganss.Xss;
using JoyfulReaperLib.MissionControl;
using JoyfulReaperLib.Sqlite;
using JoyfulReaperLib.WebStats.Sqlite;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using RandomGithub.GitHub;
using RandomGithub.Web.Options;
using RandomGithub.Web.Services;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GitHubAuthenticationHandler>();
builder.Services.AddMissionControlClient(builder.Configuration.GetSection(MissionControlClientOptions.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("random-repository", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue(
            "application/vnd.github+json"));

    client.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue("RandomGithub", "1.0"));

    client.DefaultRequestHeaders.Add(
        "X-GitHub-Api-Version",
        "2026-03-10");
})
.AddHttpMessageHandler<GitHubAuthenticationHandler>();

builder.Services.AddSingleton<GitHubRateLimitStatus>();
builder.Services.AddSingleton<HtmlSanitizer>();
builder.Services.AddSingleton<RandomRepositoryService>();


const string statsSchema = """
    CREATE TABLE IF NOT EXISTS Visitors (
        IpAddress TEXT PRIMARY KEY,
        Hits INTEGER NOT NULL DEFAULT 1,
        LastSeen TEXT
    );
    """;

var statsConnectionString = SqliteDatabaseInitializer.Initialize("randomgithub.db", statsSchema);

builder.Services.Configure<TelemetryOptions>(builder.Configuration.GetSection(TelemetryOptions.SectionName));
builder.Services.AddSingleton<VisitorIdProvider>();
builder.Services.AddJoyfulReaperSqliteHitCounter(options =>
{
    options.ConnectionString = statsConnectionString;
});

builder.Services.AddScoped<SqliteConnection>(_ =>
    new SqliteConnection(statsConnectionString));

builder.Services.AddScoped<SiteStatsService>();

var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), geolocation=(), microphone=(), payment=(), usb=()";

        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' https: data:; " +
            "style-src 'self' 'unsafe-inline'; " +
            "script-src 'self'; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none';";

        return Task.CompletedTask;
    });

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet(
    "/health/live",
    () => Results.Text("healthy", "text/plain"));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

//app.MapGet(
//    "/debug/random",
//    async (
//        RandomRepositoryService randomRepositoryService,
//        CancellationToken cancellationToken) =>
//    {
//        return await randomRepositoryService.GetRandomAsync(cancellationToken);
//    });

app.Run();
