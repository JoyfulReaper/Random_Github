using RandomGithub.GitHub;
using System.Net;
using System.Net.Http.Headers;

namespace RandomGithub.Web.Services;

public sealed class GitHubAuthenticationHandler(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var personalToken =
            httpContextAccessor.HttpContext?.Session.GetString(
                GitHubSessionKeys.PersonalAccessToken);

        var usesPersonalToken = !string.IsNullOrWhiteSpace(personalToken);

        var token = usesPersonalToken
            ? personalToken
            : configuration["GitHub:Token"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        if (usesPersonalToken)
        {
            request.Options.Set(
                GitHubRequestOptions.UsesPersonalToken,
                true);
        }

        var response = await base.SendAsync(
            request,
            cancellationToken);

        if (usesPersonalToken &&
            response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();

            throw new GitHubAuthenticationException();
        }

        return response;
    }
}