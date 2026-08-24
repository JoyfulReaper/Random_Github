using RandomGithub.GitHub;

namespace RandomGithub.Web.Services;

public sealed class RandomRepositoryService
{
    private readonly IGitHubClient _gitHubClient;
    private long _maxRepositoryId;

    public RandomRepositoryService(
        IGitHubClient gitHubClient,
        IConfiguration configuration)
    {
        _gitHubClient = gitHubClient;

        _maxRepositoryId = configuration.GetValue<long>("GitHub:InitialMaxRepositoryId");

        if (_maxRepositoryId <= 0)
        {
            throw new InvalidOperationException("GitHub:InitialMaxRepositoryId must be configured.");
        }
    }

    public async Task<GitHubRepository> GetRandomAsync(
        CancellationToken cancellationToken = default)
    {
        var since = Random.Shared.NextInt64(0, Interlocked.Read(ref _maxRepositoryId));

        var repositories =
            await _gitHubClient.GetPublicRepositoriesAfterAsync(since, cancellationToken);

        if (repositories.Count == 0)
        {
            throw new InvalidOperationException("GitHub returned no repositories.");
        }

        return repositories[Random.Shared.Next(repositories.Count)];
    }
}