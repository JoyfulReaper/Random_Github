using RandomGithub.GitHub;

namespace RandomGithub.Web.Services;

public sealed class RandomRepositoryService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly long _maxRepositoryId;
    private readonly Queue<GitHubRepository> _repositoryPool = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RandomRepositoryService(IGitHubClient gitHubClient, IConfiguration configuration)
    {
        _gitHubClient = gitHubClient;
        _maxRepositoryId = configuration.GetValue<long>("GitHub:InitialMaxRepositoryId");

        if (_maxRepositoryId <= 0)
        {
            throw new InvalidOperationException("GitHub:InitialMaxRepositoryId must be configured.");
        }
    }

    public async Task<GitHubRepository> GetRandomAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_repositoryPool.Count == 0)
            {
                await RefillPoolAsync(cancellationToken);
            }

            return _repositoryPool.Dequeue();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefillPoolAsync(CancellationToken cancellationToken)
    {
        var since = Random.Shared.NextInt64(0, _maxRepositoryId);

        var repositories = await _gitHubClient.GetPublicRepositoriesAfterAsync(
            since,
            cancellationToken);

        if (repositories.Count == 0)
        {
            throw new InvalidOperationException("GitHub returned no repositories.");
        }

        foreach (var repository in repositories.OrderBy(_ => Random.Shared.Next()))
        {
            _repositoryPool.Enqueue(repository);
        }
    }
}