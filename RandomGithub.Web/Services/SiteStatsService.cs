using JoyfulReaperLib.WebStats.Sqlite;

namespace RandomGithub.Web.Services;

public sealed class SiteStatsService(
    IHitCounter hitCounter,
    VisitorIdProvider visitorIdProvider)
{
    public async Task<SiteStats> RecordHitAsync(
        string ipAddress)
    {
        var stats = await hitCounter.RecordHitAsync(ipAddress);

        return new SiteStats(
            stats.TotalHits,
            stats.UniqueVisitors,
            visitorIdProvider.GetVisitorId(ipAddress));
    }

    public async Task<SiteStats> GetStatsAsync()
    {
        var stats = await hitCounter.GetHitCountsAsync();

        return new SiteStats(
            stats.TotalHits,
            stats.UniqueVisitors,
            VisitorId: null);
    }
}

public sealed record SiteStats(
    long TotalHits,
    long UniqueVisitors,
    string? VisitorId);