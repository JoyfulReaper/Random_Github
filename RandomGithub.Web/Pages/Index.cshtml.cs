using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;

namespace RandomGithub.Web.Pages;

public sealed class IndexModel(
    RandomRepositoryService randomRepositoryService,
    IGitHubClient gitHubClient,
    HtmlSanitizer htmlSanitizer) : PageModel
{
    public GitHubRepository? Repository { get; private set; }

    public string? ReadmeHtml { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadRepositoryAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadRepositoryAsync(cancellationToken);

        return Page();
    }

    private async Task LoadRepositoryAsync(CancellationToken cancellationToken)
    {
        Repository = await randomRepositoryService.GetRandomAsync(cancellationToken);

        var readmeHtml = await gitHubClient.GetReadmeHtmlAsync(
            Repository.Owner.Login,
            Repository.Name,
            cancellationToken);

        ReadmeHtml = readmeHtml is null
            ? null
            : htmlSanitizer.Sanitize(readmeHtml);
    }
}