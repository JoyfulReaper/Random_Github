using Ganss.Xss;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(
    (services, client) =>
    {
        var configuration = services.GetRequiredService<IConfiguration>();

        client.BaseAddress = new Uri("https://api.github.com");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("RandomGithub", "1.0"));

        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");

        var token = configuration["GitHub:Token"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    });

builder.Services.AddSingleton<HtmlSanitizer>();
builder.Services.AddSingleton<RandomRepositoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

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
