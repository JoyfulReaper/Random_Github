using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;

namespace RandomGithub.Web.Pages;

public sealed class IndexModel(
    RandomRepositoryService randomRepositoryService,
    IGitHubClient gitHubClient) : PageModel
{
    public GitHubRepository? Repository { get; private set; }

    public string? ReadmeHtml { get; private set; }

    public async Task OnGetAsync(
        CancellationToken cancellationToken)
    {
        Repository = await randomRepositoryService.GetRandomAsync(cancellationToken);

        ReadmeHtml = await gitHubClient.GetReadmeHtmlAsync(
            Repository.Owner.Login,
            Repository.Name,
            cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        Repository = await randomRepositoryService.GetRandomAsync(cancellationToken);

        ReadmeHtml = await gitHubClient.GetReadmeHtmlAsync(
            Repository.Owner.Login,
            Repository.Name,
            cancellationToken);

        return Page();
    }
}