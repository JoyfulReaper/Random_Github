namespace RandomGithub.Web.Events;

public sealed record RepositoryPickCompletedEvent(
    string? VisitorId,
    long RepositoryId,
    string FullName,
    string? Language,
    int Stars,
    int Forks,
    bool IsFork,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? PushedAt,
    bool ExcludeForks,
    bool UsedPersonalToken,
    bool HasReadme,
    long DurationMilliseconds);