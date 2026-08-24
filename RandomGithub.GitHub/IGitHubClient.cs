namespace RandomGithub.GitHub;

public interface IGitHubClient
{
    Task<IReadOnlyList<GitHubRepository>>
        GetPublicRepositoriesAfterAsync(
            long repositoryId,
            CancellationToken cancellationToken = default);
}