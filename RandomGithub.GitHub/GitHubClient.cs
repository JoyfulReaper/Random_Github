using System.Net;
using System.Net.Http.Json;

namespace RandomGithub.GitHub;

public sealed class GitHubClient(HttpClient httpClient) : IGitHubClient
{
    public async Task<IReadOnlyList<GitHubRepository>>
        GetPublicRepositoriesAfterAsync(
            long repositoryId,
            CancellationToken cancellationToken = default)
    {
        if (repositoryId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repositoryId));
        }

        var repositories =
            await httpClient.GetFromJsonAsync<List<GitHubRepository>>(
                $"/repositories?since={repositoryId}",
                cancellationToken);

        return repositories ?? [];
    }

    public async Task<string?> GetReadmeHtmlAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/repos/{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/readme");

        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("application/vnd.github.html+json");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}