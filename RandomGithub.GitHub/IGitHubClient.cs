namespace RandomGithub.GitHub;

public interface IGitHubClient
{
    Task<IReadOnlyList<GitHubRepository>>
        GetPublicRepositoriesAfterAsync(
            long repositoryId,
            CancellationToken cancellationToken = default);

    Task<string?> GetReadmeHtmlAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default);

    Task<GitHubRepository?> GetRepositoryAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default);
}