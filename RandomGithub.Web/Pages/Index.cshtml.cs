using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;

namespace RandomGithub.Web.Pages;

public sealed class IndexModel(
    RandomRepositoryService randomRepositoryService) : PageModel
{
    public GitHubRepository? Repository { get; private set; }

    public async Task OnGetAsync(
        CancellationToken cancellationToken)
    {
        Repository = await randomRepositoryService.GetRandomAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        Repository = await randomRepositoryService.GetRandomAsync(cancellationToken);

        return Page();
    }
}