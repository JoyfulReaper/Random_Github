using System.Text.Json.Serialization;

namespace RandomGithub.Web.Events;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RepositoryPickCompletedEvent))]
[JsonSerializable(typeof(RepositorySelfPickEvent))]
public partial class RandomGithubJsonContext : JsonSerializerContext
{
}