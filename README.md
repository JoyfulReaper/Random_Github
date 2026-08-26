# Random GitHub

Random GitHub picks a random public GitHub repository and shows you whatever strange corner of GitHub you probably had no reason to ever find.

**Live:** https://randomgit.kgivler.com

This is a rewrite of a small project I originally made in 2021.

## Features

- Random public repository discovery
- Repository metadata including language, stars, forks, and dates
- Rendered and sanitized README display
- Optional **Skip forks** filtering
- Shared GitHub API allowance
- Optional bring-your-own GitHub personal access token
- Friendly GitHub API rate-limit handling
- Per-client request rate limiting
- Loading feedback while a repository is being selected
- Public visit, unique-visitor, and repositories-served statistics
- Privacy-preserving visitor IDs for telemetry
- Mission Control telemetry for successful repository picks
- A very unlikely self-pick easter egg if Random GitHub randomly selects itself
- Automated service-level tests

## How random selection works

GitHub exposes a public repository listing endpoint that accepts a repository ID using the `since` parameter.

Random GitHub:

1. Chooses random repository IDs across a configured ID range.
2. Requests the public repositories following those IDs.
3. Builds a shuffled in-memory pool from several batches.
4. De-duplicates repositories by GitHub repository ID.
5. Returns repositories from that pool.
6. Refills the pool as it gets smaller.

An empty pool is initially filled from four GitHub API batches.

After that, refilling uses two levels:

- Below half capacity, there is a 1-in-10 chance per pick of fetching another batch.
- Between half and full capacity, there is a 1-in-100 chance per pick of fetching another batch.

This keeps the pool changing over time without requiring another GitHub API request for every random pick.

Random GitHub does not use GitHub's search API for normal repository discovery.

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

## Public statistics

Random GitHub keeps a small set of public usage statistics:

- Total visits
- Unique visitors
- Random repositories served

A visit is counted once per ASP.NET Core session rather than every time the **Random Repository** button is pressed.

Unique visitors are tracked by the local `JoyfulReaperLib.WebStats.Sqlite` hit counter.

The random-repositories-served counter increases whenever Random GitHub successfully produces a repository for the user, including when the detailed GitHub lookup fails and the queued repository data is used as a fallback.

These statistics are stored in a local SQLite database.

## Privacy-preserving visitor telemetry

Random GitHub can correlate activity from repeat visitors in Mission Control without publishing raw IP addresses.

For telemetry, the visitor IP address is normalized and passed through HMAC-SHA256 using a server-side secret key.

Conceptually:

```text
normalized IP
    |
    v
HMAC-SHA256(server secret, IP bytes)
    |
    v
visitor ID
````

Only the resulting visitor ID is included in Mission Control events.

The secret hashing key is never included in telemetry and should never be committed to the repository.

IPv4-mapped IPv6 addresses are normalized to IPv4 before hashing so equivalent addresses produce the same visitor ID.

If no visitor hashing key is configured, visitor IDs are omitted from telemetry.

## Mission Control

Random GitHub can optionally publish repository-selection telemetry to my private Mission Control service using `JoyfulReaperLib.MissionControl`.

A repository-pick event may contain:

* Privacy-preserving visitor ID
* Repository ID
* Repository name
* Language
* Star and fork counts
* Fork status
* Creation and last-push dates
* Whether **Skip forks** was enabled
* Whether a personal token was being used
* Whether a README was available
* Selection duration

It does **not** include:

* The user's GitHub token
* The user's GitHub account identity
* The visitor's raw IP address

If Random GitHub naturally selects its own repository, a separate `repository-self-pick` telemetry event is also published using the same correlation ID as the normal repository-pick event.

The self-pick has no increased probability or special weighting. It has to happen naturally.

Mission Control is optional and disabled by default.

## Projects

The solution contains three projects:

### `RandomGithub.GitHub`

Reusable GitHub API client and GitHub response models.

### `RandomGithub.Web`

ASP.NET Core Razor Pages application containing repository selection, UI, session handling, public statistics, rate limiting, README sanitization, privacy-preserving visitor IDs, and Mission Control telemetry.

### `RandomGithub.Tests`

xUnit test project covering service-level behavior including:

* Visitor ID hashing and address normalization
* Site statistics and the repositories-served counter
* Initial repository pool loading
* Fork filtering
* Repository de-duplication

## Requirements

* .NET 10 SDK
* A GitHub personal access token is recommended for development

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

Configure a visitor hashing secret if you want visitor IDs in local Mission Control telemetry:

```powershell
dotnet user-secrets set "Telemetry:VisitorHashKey" "use-a-long-random-secret-here"
```

Then run:

```powershell
dotnet run
```

Secrets should not be committed to `appsettings.json`.

Run the test suite from the repository root with:

```powershell
dotnet test
```

## Configuration

The main configuration looks like:

```json
{
  "GitHub": {
    "InitialMaxRepositoryId": 1100000000,
    "Token": ""
  },
  "Telemetry": {
    "VisitorHashKey": ""
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
Telemetry__VisitorHashKey
MissionControl__ApiKey
MissionControl__CloudflareAccessClientId
MissionControl__CloudflareAccessClientSecret
```

The Cloudflare Access credentials are optional and are only required when the Mission Control endpoint itself is protected by a Cloudflare Access service-token policy.

## Security notes

Rendered GitHub README HTML is sanitized before being written to the page.

The application also includes:

* Per-client request rate limiting
* Content Security Policy
* Frame protection
* MIME sniffing protection
* Referrer restrictions
* Permissions Policy
* Trusted forwarded-header support for deployment behind a reverse proxy

Inline JavaScript is not permitted by the application's Content Security Policy; application JavaScript is served from local static files.

Production proxy trust and allowed-host configuration must be configured for the actual deployment environment.

## Privacy

Random GitHub does not require an account and does not use advertising or third-party analytics.

Public visitor statistics contain aggregate counts only.

Mission Control uses a keyed visitor hash to correlate repeat activity without publishing raw visitor IP addresses.

See the site's **Privacy** page for additional details about sessions, telemetry, cookies, local statistics, and external requests.

## License

See `LICENSE.md` for license information.
