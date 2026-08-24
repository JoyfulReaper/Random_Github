namespace RandomGithub.GitHub;

public sealed record GitHubRateLimitSnapshot(
    int Limit,
    int Remaining,
    DateTimeOffset ResetAt,
    string Resource);

public sealed class GitHubRateLimitStatus
{
    private GitHubRateLimitSnapshot? _latest;

    public GitHubRateLimitSnapshot? Latest => Volatile.Read(ref _latest);

    internal void Update(HttpResponseMessage response)
    {
        if (!TryGetIntHeader(response, "X-RateLimit-Limit", out var limit) ||
            !TryGetIntHeader(response, "X-RateLimit-Remaining", out var remaining) ||
            !TryGetLongHeader(response, "X-RateLimit-Reset", out var resetUnix))
        {
            return;
        }

        var resource = "unknown";

        if (response.Headers.TryGetValues("X-RateLimit-Resource", out var resourceValues))
        {
            resource = resourceValues.FirstOrDefault() ?? resource;
        }

        var snapshot = new GitHubRateLimitSnapshot(
            limit,
            remaining,
            DateTimeOffset.FromUnixTimeSeconds(resetUnix),
            resource);

        Interlocked.Exchange(ref _latest, snapshot);
    }

    private static bool TryGetIntHeader(
        HttpResponseMessage response,
        string name,
        out int value)
    {
        value = 0;

        return response.Headers.TryGetValues(name, out var values) &&
               int.TryParse(values.FirstOrDefault(), out value);
    }

    private static bool TryGetLongHeader(
        HttpResponseMessage response,
        string name,
        out long value)
    {
        value = 0;

        return response.Headers.TryGetValues(name, out var values) &&
               long.TryParse(values.FirstOrDefault(), out value);
    }
}