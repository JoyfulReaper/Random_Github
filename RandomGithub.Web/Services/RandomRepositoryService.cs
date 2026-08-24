using RandomGithub.GitHub;

namespace RandomGithub.Web.Services;

public sealed class RandomRepositoryService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly long _maxRepositoryId;
    private readonly Queue<GitHubRepository> _repositoryPool = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int PoolBatchCount = 4;

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
        var repositories = new List<GitHubRepository>();

        for (var i = 0; i < PoolBatchCount; i++)
        {
            var since = Random.Shared.NextInt64(0, _maxRepositoryId);

            var batch = await _gitHubClient.GetPublicRepositoriesAfterAsync(
                since,
                cancellationToken);

            repositories.AddRange(batch);
        }

        var shuffledRepositories = repositories
            .DistinctBy(repository => repository.Id)
            .ToArray();

        Random.Shared.Shuffle(shuffledRepositories);

        if (shuffledRepositories.Length == 0)
        {
            throw new InvalidOperationException("GitHub returned no repositories.");
        }

        foreach (var repository in shuffledRepositories)
        {
            _repositoryPool.Enqueue(repository);
        }
    }
}