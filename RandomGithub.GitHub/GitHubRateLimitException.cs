namespace RandomGithub.GitHub;

public sealed class GitHubRateLimitException(DateTimeOffset? retryAt = null)
    : Exception("GitHub API rate limit exceeded.")
{
    public DateTimeOffset? RetryAt { get; } = retryAt;
}