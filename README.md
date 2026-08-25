# Random GitHub

Random GitHub picks a random public GitHub repository and shows you whatever strange corner of GitHub you probably had no reason to ever find.

This is a rewrite of a small project I originally made in 2021.

## Features

- Random public repository discovery
- Repository metadata including language, stars, forks, and dates
- Rendered and sanitized README display
- Optional **Skip forks** filtering
- Shared GitHub API allowance
- Optional bring-your-own GitHub personal access token
- Friendly GitHub API rate-limit handling
- Request rate limiting
- Mission Control telemetry for successful repository picks

## How random selection works

GitHub exposes a public repository listing endpoint that accepts a repository ID using the `since` parameter.

Random GitHub:

1. Chooses random repository IDs across a configured ID range.
2. Requests the public repositories following those IDs.
3. Builds a shuffled in-memory pool from several batches.
4. Returns repositories from that pool.
5. Opportunistically refills the pool as it gets smaller.

This avoids using GitHub's search API for normal repository discovery.

The selection is intentionally simple and is not mathematically uniform across every GitHub repository. Repository IDs contain gaps, so repositories following larger gaps have a somewhat greater chance of being selected.

For this project, that is fine. The goal is discovery, not cryptographic-grade repository roulette.

## GitHub API usage

Random GitHub normally uses a shared GitHub API token configured by the server operator.

GitHub API quota information for the shared token is displayed in the site footer.

If the shared allowance is exhausted, users may optionally provide their own fine-grained GitHub personal access token.

The token only needs:

- **Public repositories**
- No additional repository permissions
- No user/account permissions

A user-provided token is stored only in the ASP.NET Core server-side session. The browser receives a session identifier rather than the token itself.

The token is removed when:

- The user selects **Forget token**
- The session expires
- GitHub rejects the token

User-provided tokens are not included in Mission Control telemetry.

The source responsible for applying the token to GitHub requests is:

`RandomGithub.Web/Services/GitHubAuthenticationHandler.cs`

## Mission Control

Random GitHub can optionally publish successful repository selections to my private Mission Control telemetry service using `JoyfulReaperLib.MissionControl`.

A repository-pick event may contain:

- Repository ID
- Repository name
- Language
- Star and fork counts
- Fork status
- Creation and last-push dates
- Whether **Skip forks** was enabled
- Whether a personal token was being used
- Whether a README was available
- Selection duration

It does **not** include the user's GitHub token or GitHub account identity.

Mission Control is optional and disabled by default.

## Projects

The solution contains two projects:

### `RandomGithub.GitHub`

Reusable GitHub API client and GitHub response models.

### `RandomGithub.Web`

ASP.NET Core Razor Pages application containing repository selection, UI, session token handling, rate limiting, README sanitization, and telemetry.

## Requirements

- .NET 10 SDK
- A GitHub personal access token is recommended for development

## Running locally

Clone the repository:

```powershell
git clone https://github.com/JoyfulReaper/Random_Github.git
cd Random_Github
```

Configure a GitHub token using user secrets:

```powershell
cd RandomGithub.Web
dotnet user-secrets set "GitHub:Token" "github_pat_..."
```

Then run:

```powershell
dotnet run
```

The token should not be committed to `appsettings.json`.

## Configuration

The main configuration looks like:

```json
{
  "GitHub": {
    "InitialMaxRepositoryId": 1100000000,
    "Token": ""
  },
  "MissionControl": {
    "Enabled": false,
    "BaseUrl": "https://missioncontrol.example.com",
    "ApiKey": "",
    "CloudflareAccessClientId": "",
    "CloudflareAccessClientSecret": "",
    "TimeoutMilliseconds": 1000
  }
}
```

For production, secrets should be supplied through environment variables or another secret store, for example:

```text
GitHub__Token
MissionControl__ApiKey
MissionControl__CloudflareAccessClientId
MissionControl__CloudflareAccessClientSecret
```

The Cloudflare Access credentials are optional and are only required when the Mission Control endpoint itself is protected by a Cloudflare Access service-token policy.

## Security notes

Rendered GitHub README HTML is sanitized before being written to the page.

The application also includes:

- Per-client request rate limiting
- Content Security Policy
- Frame protection
- MIME sniffing protection
- Referrer restrictions
- Permissions Policy
- Trusted forwarded-header support for deployment behind a reverse proxy

Production proxy trust and allowed-host configuration must be configured for the actual deployment environment.

## Privacy

Random GitHub does not require an account and does not use advertising or third-party analytics.

See the site's **Privacy** page for details about session tokens, Mission Control telemetry, cookies, and external requests.

## License

See `LICENSE` for license information.
