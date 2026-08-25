using Ganss.Xss;
using JoyfulReaperLib.MissionControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using RandomGithub.GitHub;
using RandomGithub.Web.Events;
using RandomGithub.Web.Services;
using System.Diagnostics;

namespace RandomGithub.Web.Pages;

[EnableRateLimiting("random-repository")]
public sealed class IndexModel(
    RandomRepositoryService randomRepositoryService,
    IGitHubClient gitHubClient,
    HtmlSanitizer htmlSanitizer,
    IMissionControlClient missionControlClient,
    ILogger<IndexModel> logger) : PageModel
{
    public GitHubRepository? Repository { get; private set; }
    public string? ReadmeHtml { get; private set; }
    public bool GitHubRateLimited { get; private set; }
    public DateTimeOffset? GitHubRetryAt { get; private set; }

    public bool PersonalTokenInvalid { get; private set; }
    public bool UsingPersonalAccessToken =>
        HttpContext.Session.GetString(GitHubSessionKeys.PersonalAccessToken) is not null;

    private const string SelfRepository = "JoyfulReaper/Random_Github";

    [BindProperty(SupportsGet = true)]
    public bool ExcludeForks { get; set; }

    [BindProperty]
    public string? PersonalAccessToken { get; set; }

    public async Task OnGetAsync(
        CancellationToken cancellationToken)
    {
        await LoadRepositoryAsync(cancellationToken);
    }

    public IActionResult OnPostUseToken()
    {
        if (!string.IsNullOrWhiteSpace(PersonalAccessToken))
        {
            HttpContext.Session.SetString(
                GitHubSessionKeys.PersonalAccessToken,
                PersonalAccessToken.Trim());
        }

        PersonalAccessToken = null;

        return RedirectToPage();
    }

    public IActionResult OnPostForgetToken()
    {
        HttpContext.Session.Remove(GitHubSessionKeys.PersonalAccessToken);

        return RedirectToPage();
    }

    private async Task LoadRepositoryAsync(
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var candidate = await randomRepositoryService.GetRandomAsync(
                cancellationToken,
                excludeForks: ExcludeForks);

            Repository = await gitHubClient.GetRepositoryAsync(
                candidate.Owner.Login,
                candidate.Name,
                cancellationToken);

            if (Repository is null)
            {
                Repository = candidate;
                ReadmeHtml = null;
                return;
            }

            var readmeHtml = await gitHubClient.GetReadmeHtmlAsync(
                Repository.Owner.Login,
                Repository.Name,
                cancellationToken);

            ReadmeHtml = readmeHtml is null
                ? null
                : htmlSanitizer.Sanitize(readmeHtml);

            stopwatch.Stop();

            await PublishRepositoryPickCompletedAsync(
                stopwatch.ElapsedMilliseconds,
                occurredAt,
                correlationId);

            if (IsSelfPick)
            {
                await PublishRepositorySelfPickAsync(occurredAt, correlationId);
            }
        }
        catch (GitHubAuthenticationException)
        {
            HttpContext.Session.Remove(GitHubSessionKeys.PersonalAccessToken);

            Repository = null;
            ReadmeHtml = null;
            PersonalTokenInvalid = true;
        }
        catch (GitHubRateLimitException exception)
        {
            Repository = null;
            ReadmeHtml = null;
            GitHubRateLimited = true;
            GitHubRetryAt = exception.RetryAt;
        }
    }

    private async Task PublishRepositoryPickCompletedAsync(
        long durationMilliseconds,
        DateTimeOffset occurredAt,
        string correlationId)
    {
        if (Repository is null)
        {
            return;
        }

        try
        {
            await missionControlClient.TryPublishAsync(
                eventType: RandomGithubEventTypes.RepositoryPickCompleted,
                payload: new RepositoryPickCompletedEvent(
                    RepositoryId: Repository.Id,
                    FullName: Repository.FullName,
                    Language: Repository.Language,
                    Stars: Repository.StargazersCount,
                    Forks: Repository.ForksCount,
                    IsFork: Repository.IsFork,
                    CreatedAt: Repository.CreatedAt,
                    PushedAt: Repository.PushedAt,
                    ExcludeForks: ExcludeForks,
                    UsedPersonalToken: UsingPersonalAccessToken,
                    HasReadme: !string.IsNullOrWhiteSpace(ReadmeHtml),
                    DurationMilliseconds: durationMilliseconds),
                occurredAt: occurredAt,
                payloadTypeInfo:
                    RandomGithubJsonContext.Default.RepositoryPickCompletedEvent,
                correlationId: correlationId,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to publish repository-pick event {CorrelationId}.",
                correlationId);
        }
    }

    private async Task PublishRepositorySelfPickAsync(
        DateTimeOffset occurredAt,
        string correlationId)
    {
        if (Repository is null)
        {
            return;
        }

        try
        {
            await missionControlClient.TryPublishAsync(
                eventType: RandomGithubEventTypes.RepositorySelfPick,
                payload: new RepositorySelfPickEvent(
                    RepositoryId: Repository.Id,
                    FullName: Repository.FullName,
                    ExcludeForks: ExcludeForks,
                    UsedPersonalToken: UsingPersonalAccessToken),
                occurredAt: occurredAt,
                payloadTypeInfo: RandomGithubJsonContext.Default.RepositorySelfPickEvent,
                correlationId: correlationId,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to publish repository self-pick event {CorrelationId}.",
                correlationId);
        }
    }

    public bool IsSelfPick =>
        string.Equals(Repository?.FullName, SelfRepository, StringComparison.OrdinalIgnoreCase);
}