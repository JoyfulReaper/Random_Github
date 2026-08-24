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
}