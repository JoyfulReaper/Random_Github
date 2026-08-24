using System.Net;
using System.Net.Http.Json;

namespace RandomGithub.GitHub;

public sealed class GitHubClient(
    HttpClient httpClient,
    GitHubRateLimitStatus rateLimitStatus) : IGitHubClient
{
    public async Task<IReadOnlyList<GitHubRepository>>
        GetPublicRepositoriesAfterAsync(
            long repositoryId,
            CancellationToken cancellationToken = default)
    {
        if (repositoryId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(repositoryId));
        }

        using var response = await httpClient.GetAsync(
            $"/repositories?since={repositoryId}&per_page=100",
            cancellationToken);

        rateLimitStatus.Update(response);

        await ThrowIfRateLimitedAsync(response, cancellationToken);

        response.EnsureSuccessStatusCode();

        var repositories =
            await response.Content.ReadFromJsonAsync<List<GitHubRepository>>(cancellationToken);

        return repositories ?? [];
    }

    public async Task<string?> GetReadmeHtmlAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/readme");

        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("application/vnd.github.html+json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        rateLimitStatus.Update(response);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfRateLimitedAsync(response, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task ThrowIfRateLimitedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is not
            (HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests))
        {
            return;
        }

        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
            remaining.FirstOrDefault() == "0")
        {
            DateTimeOffset? resetAt = null;

            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
                long.TryParse(resetValues.FirstOrDefault(), out var resetUnix))
            {
                resetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
            }

            throw new GitHubRateLimitException(resetAt);
        }

        if (response.Headers.RetryAfter?.Delta is { } retryAfter)
        {
            throw new GitHubRateLimitException(DateTimeOffset.UtcNow.Add(retryAfter));
        }

        if (response.Headers.RetryAfter?.Date is { } retryAt)
        {
            throw new GitHubRateLimitException(retryAt);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new GitHubRateLimitException(DateTimeOffset.UtcNow.AddMinutes(1));
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (responseBody.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            throw new GitHubRateLimitException(DateTimeOffset.UtcNow.AddMinutes(1));
        }
    }
}