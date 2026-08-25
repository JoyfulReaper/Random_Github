namespace RandomGithub.Web.Events;

public sealed record RepositorySelfPickEvent(
    long RepositoryId,
    string FullName,
    bool ExcludeForks,
    bool UsedPersonalToken);