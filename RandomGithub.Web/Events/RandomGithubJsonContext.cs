using System.Text.Json.Serialization;

namespace RandomGithub.Web.Events;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RepositoryPickCompletedEvent))]
public partial class RandomGithubJsonContext : JsonSerializerContext
{
}