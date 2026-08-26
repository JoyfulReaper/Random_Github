using JoyfulReaperLib.WebStats.Sqlite;
using Microsoft.Data.Sqlite;

namespace RandomGithub.Web.Services;

public sealed class SiteStatsService(
    IHitCounter hitCounter,
    VisitorIdProvider visitorIdProvider,
    SqliteConnection dbConnection)
{
    public async Task<SiteStats> RecordHitAsync(string ipAddress)
    {
        var visitorId = visitorIdProvider.GetVisitorId(ipAddress);

        if (visitorId is null)
        {
            throw new InvalidOperationException("Visitor hashing must be configured to record visitor statistics.");
        }

        var stats = await hitCounter.RecordHitAsync(visitorId);
        var randomRepositoriesServed = await GetRandomRepositoriesServedAsync();

        return new SiteStats(
            stats.TotalHits,
            stats.UniqueVisitors,
            randomRepositoriesServed,
            visitorId);
    }

    public async Task<SiteStats> GetStatsAsync()
    {
        var stats = await hitCounter.GetHitCountsAsync();
        var randomRepositoriesServed = await GetRandomRepositoriesServedAsync();

        return new SiteStats(
            stats.TotalHits,
            stats.UniqueVisitors,
            randomRepositoriesServed,
            VisitorId: null);
    }

    public async Task IncrementRandomRepositoriesServedAsync()
    {
        await EnsureConnectionOpenAsync();

        await using var command = dbConnection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppStats (
                Id,
                RandomRepositoriesServed
            )
            VALUES (1, 1)
            ON CONFLICT(Id) DO UPDATE SET
                RandomRepositoriesServed =
                    RandomRepositoriesServed + 1;
            """;

        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> GetRandomRepositoriesServedAsync()
    {
        await EnsureConnectionOpenAsync();

        await using var command = dbConnection.CreateCommand();
        command.CommandText = """
            SELECT RandomRepositoriesServed
            FROM AppStats
            WHERE Id = 1;
            """;

        var result = await command.ExecuteScalarAsync();

        return result is null or DBNull
            ? 0
            : Convert.ToInt64(result);
    }

    private async Task EnsureConnectionOpenAsync()
    {
        if (dbConnection.State !=
            System.Data.ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }
    }
}

public sealed record SiteStats(
    long TotalHits,
    long UniqueVisitors,
    long RandomRepositoriesServed,
    string? VisitorId);