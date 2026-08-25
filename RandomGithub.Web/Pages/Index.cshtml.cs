using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;

namespace RandomGithub.Web.Pages;

[EnableRateLimiting("random-repository")]
public sealed class IndexModel(
    RandomRepositoryService randomRepositoryService,
    IGitHubClient gitHubClient,
    HtmlSanitizer htmlSanitizer) : PageModel
{
    public GitHubRepository? Repository { get; private set; }

    public string? ReadmeHtml { get; private set; }

    public bool GitHubRateLimited { get; private set; }

    public DateTimeOffset? GitHubRetryAt { get; private set; }

    public bool PersonalTokenInvalid { get; private set; }

    public bool UsingPersonalAccessToken =>
        HttpContext.Session.GetString(
            GitHubSessionKeys.PersonalAccessToken) is not null;

    [BindProperty]
    public bool ExcludeForks { get; set; }

    [BindProperty]
    public string? PersonalAccessToken { get; set; }

    public async Task OnGetAsync(
        CancellationToken cancellationToken)
    {
        await LoadRepositoryAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        await LoadRepositoryAsync(cancellationToken);

        return Page();
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
        HttpContext.Session.Remove(
            GitHubSessionKeys.PersonalAccessToken);

        return RedirectToPage();
    }

    private async Task LoadRepositoryAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var candidate =
                await randomRepositoryService.GetRandomAsync(
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
        }
        catch (GitHubAuthenticationException)
        {
            HttpContext.Session.Remove(
                GitHubSessionKeys.PersonalAccessToken);

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
}