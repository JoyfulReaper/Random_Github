namespace RandomGithub.Web.Events;

public sealed record RepositorySelfPickEvent(
    string? VisitorId,
    long RepositoryId,
    string FullName,
    bool ExcludeForks,
    bool UsedPersonalToken);