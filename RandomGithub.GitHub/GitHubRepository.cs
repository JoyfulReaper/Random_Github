using System.Text.Json.Serialization;

namespace RandomGithub.GitHub;

public sealed class GitHubRepository
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Language { get; init; }

    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; init; }

    [JsonPropertyName("forks_count")]
    public int ForksCount { get; init; }

    public GitHubOwner Owner { get; init; } = new();
}

public sealed class GitHubOwner
{
    public string Login { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; init; } = string.Empty;
}