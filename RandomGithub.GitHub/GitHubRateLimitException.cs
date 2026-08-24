namespace RandomGithub.GitHub;

public sealed class GitHubRateLimitException(
    DateTimeOffset? resetAt = null)
    : Exception("GitHub API rate limit exceeded.")
{
    public DateTimeOffset? ResetAt { get; } = resetAt;
}