using RandomGithub.GitHub;

namespace RandomGithub.Web.Services;

public sealed class RandomRepositoryService(
    IGitHubClient gitHubClient,
    IConfiguration configuration)
{
    public async Task<GitHubRepository> GetRandomAsync(
        CancellationToken cancellationToken = default)
    {
        var maxRepositoryId = configuration.GetValue<long>("GitHub:MaxRepositoryId");

        if (maxRepositoryId <= 0)
        {
            throw new InvalidOperationException("GitHub:MaxRepositoryId must be configured.");
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var since = Random.Shared.NextInt64(0, maxRepositoryId);

            var repositories =
                await gitHubClient.GetPublicRepositoriesAfterAsync(since, cancellationToken);

            if (repositories.Count == 0)
            {
                continue;
            }

            return repositories[Random.Shared.Next(repositories.Count)];
        }

        throw new InvalidOperationException("Unable to find a random public repository.");
    }
}