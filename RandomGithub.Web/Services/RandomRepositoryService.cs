using RandomGithub.GitHub;

namespace RandomGithub.Web.Services;

public sealed class RandomRepositoryService
{
    private const int PoolBatchCount = 4;
    private const int RepositoriesPerBatch = 100;
    private const int LowWaterRefillOneIn = 10;
    private const int RandomRefillOneIn = 100;

    private readonly IGitHubClient _gitHubClient;
    private readonly long _maxRepositoryId;
    private readonly Queue<GitHubRepository> _repositoryPool = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<RandomRepositoryService> _logger;

    public RandomRepositoryService(
        IGitHubClient gitHubClient,
        IConfiguration configuration,
        ILogger<RandomRepositoryService> logger)
    {
        _gitHubClient = gitHubClient;
        _logger = logger;

        if (!long.TryParse(configuration["GitHub:InitialMaxRepositoryId"], out _maxRepositoryId) ||
            _maxRepositoryId <= 0)
        {
            throw new InvalidOperationException("GitHub:InitialMaxRepositoryId must be configured.");
        }
    }

    public async Task<GitHubRepository> GetRandomAsync(
        CancellationToken cancellationToken = default,
        bool excludeForks = false)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_repositoryPool.Count == 0)
            {
                await AddRandomBatchesAsync(PoolBatchCount, cancellationToken);
            }
            else if (ShouldRefill())
            {
                try
                {
                    await AddRandomBatchesAsync(1, cancellationToken);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "GitHub repository pool refill failed. Continuing with {RepositoryCount} cached repositories.",
                        _repositoryPool.Count);
                }
                catch (GitHubRateLimitException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "GitHub repository pool refill was rate limited. Continuing with {RepositoryCount} cached repositories.",
                        _repositoryPool.Count);
                }
            }

            while (true)
            {
                while (_repositoryPool.Count > 0)
                {
                    var repository = _repositoryPool.Dequeue();

                    if (!excludeForks || !repository.IsFork)
                    {
                        return repository;
                    }
                }

                await AddRandomBatchesAsync(1, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool ShouldRefill()
    {
        var capacity = PoolBatchCount * RepositoriesPerBatch;
        var halfCapacity = capacity / 2;

        if (_repositoryPool.Count < halfCapacity)
        {
            return Random.Shared.Next(LowWaterRefillOneIn) == 0;
        }

        return _repositoryPool.Count < capacity &&
               Random.Shared.Next(RandomRefillOneIn) == 0;
    }

    private async Task AddRandomBatchesAsync(int batchCount, CancellationToken cancellationToken)
    {
        var repositories = _repositoryPool.ToList();

        for (var i = 0; i < batchCount; i++)
        {
            var since = Random.Shared.NextInt64(0, _maxRepositoryId);
            var batch = await _gitHubClient.GetPublicRepositoriesAfterAsync(since, cancellationToken);

            repositories.AddRange(batch);
        }

        var shuffledRepositories = repositories
            .DistinctBy(repository => repository.Id)
            .ToArray();

        if (shuffledRepositories.Length == 0)
        {
            throw new InvalidOperationException("GitHub returned no repositories.");
        }

        Random.Shared.Shuffle(shuffledRepositories);

        _repositoryPool.Clear();

        foreach (var repository in shuffledRepositories)
        {
            _repositoryPool.Enqueue(repository);
        }
    }
}