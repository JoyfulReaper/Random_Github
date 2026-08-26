using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RandomGithub.GitHub;
using RandomGithub.Web.Services;

namespace RandomGithub.Tests;

public sealed class RandomRepositoryServiceTests
{
    [Fact]
    public async Task GetRandomAsync_EmptyPool_LoadsInitialFourBatches()
    {
        var client = new StubGitHubClient(call =>
            [Repository(call)]);
        var service = CreateService(client);

        var result = await service.GetRandomAsync(
            TestContext.Current.CancellationToken);

        Assert.InRange(result.Id, 1, 4);
        Assert.Equal(4, client.PublicRepositoryCalls);
    }

    [Fact]
    public async Task GetRandomAsync_ExcludeForks_SkipsForkRepositories()
    {
        var fork = Repository(1, isFork: true);
        var nonFork = Repository(2, isFork: false);
        var client = new StubGitHubClient(call =>
            call <= 4 ? [fork] : [nonFork]);
        var service = CreateService(client);

        var result = await service.GetRandomAsync(
            TestContext.Current.CancellationToken,
            excludeForks: true);

        Assert.False(result.IsFork);
        Assert.Equal(2, result.Id);
        Assert.Equal(5, client.PublicRepositoryCalls);
    }

    [Fact]
    public async Task GetRandomAsync_MergedBatches_DeDuplicatesRepositoryIds()
    {
        var duplicate = Repository(1);
        var client = new StubGitHubClient(_ => [duplicate]);
        var service = CreateService(client);

        var first = await service.GetRandomAsync(
            TestContext.Current.CancellationToken);
        var second = await service.GetRandomAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, first.Id);
        Assert.Equal(1, second.Id);
        Assert.Equal(8, client.PublicRepositoryCalls);
    }

    private static RandomRepositoryService CreateService(IGitHubClient client)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:InitialMaxRepositoryId"] = "1000000"
            })
            .Build();

        return new RandomRepositoryService(
            client,
            configuration,
            NullLogger<RandomRepositoryService>.Instance);
    }

    private static GitHubRepository Repository(long id, bool isFork = false) =>
        new()
        {
            Id = id,
            Name = $"repository-{id}",
            FullName = $"owner/repository-{id}",
            IsFork = isFork
        };

    private sealed class StubGitHubClient(
        Func<int, IReadOnlyList<GitHubRepository>> getRepositories) : IGitHubClient
    {
        public int PublicRepositoryCalls { get; private set; }

        public Task<IReadOnlyList<GitHubRepository>> GetPublicRepositoriesAfterAsync(
            long repositoryId,
            CancellationToken cancellationToken = default)
        {
            PublicRepositoryCalls++;
            return Task.FromResult(getRepositories(PublicRepositoryCalls));
        }

        public Task<string?> GetReadmeHtmlAsync(
            string owner,
            string repository,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<GitHubRepository?> GetRepositoryAsync(
            string owner,
            string repository,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<GitHubRepository?>(null);
    }
}
