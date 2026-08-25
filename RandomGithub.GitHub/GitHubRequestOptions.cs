namespace RandomGithub.GitHub;

public static class GitHubRequestOptions
{
    public static readonly HttpRequestOptionsKey<bool> UsesPersonalToken =
        new("RandomGithub.UsesPersonalToken");
}