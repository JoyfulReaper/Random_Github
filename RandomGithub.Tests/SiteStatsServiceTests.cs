using JoyfulReaperLib.WebStats.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RandomGithub.Web.Options;
using RandomGithub.Web.Services;

namespace RandomGithub.Tests;

public sealed class SiteStatsServiceTests
{
    static SiteStatsServiceTests()
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
    }

    [Fact]
    public async Task RecordHitAsync_ReturnsCountsFromHitCounter()
    {
        var hitCounter = new StubHitCounter(
            recordResult: new HitCountStats(42, 7),
            getResult: new HitCountStats(0, 0));

        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(hitCounter, database.Connection);

        var result = await service.RecordHitAsync("192.0.2.10");

        Assert.Equal(42, result.TotalHits);
        Assert.Equal(7, result.UniqueVisitors);
        Assert.Equal(1, hitCounter.RecordCalls);
    }

    [Fact]
    public async Task GetStatsAsync_DoesNotRecordAHit()
    {
        var hitCounter = new StubHitCounter(
            recordResult: new HitCountStats(99, 99),
            getResult: new HitCountStats(12, 4));

        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(hitCounter, database.Connection);

        var result = await service.GetStatsAsync();

        Assert.Equal(12, result.TotalHits);
        Assert.Equal(4, result.UniqueVisitors);
        Assert.Equal(0, hitCounter.RecordCalls);
        Assert.Equal(1, hitCounter.GetCalls);
    }

    [Fact]
    public async Task IncrementRandomRepositoriesServedAsync_IncrementsAppStatsCounter()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(new StubHitCounter(), database.Connection);

        await service.IncrementRandomRepositoriesServedAsync();

        Assert.Equal(1, await database.GetRandomRepositoriesServedAsync());
    }

    [Fact]
    public async Task IncrementRandomRepositoriesServedAsync_RepeatedCallsAccumulate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(new StubHitCounter(), database.Connection);

        await service.IncrementRandomRepositoriesServedAsync();
        await service.IncrementRandomRepositoriesServedAsync();
        await service.IncrementRandomRepositoriesServedAsync();

        Assert.Equal(3, await database.GetRandomRepositoriesServedAsync());
    }

    [Fact]
    public async Task GetStatsAsync_IncludesRandomRepositoriesServed()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(new StubHitCounter(), database.Connection);
        await service.IncrementRandomRepositoriesServedAsync();
        await service.IncrementRandomRepositoriesServedAsync();

        var result = await service.GetStatsAsync();

        Assert.Equal(2, result.RandomRepositoriesServed);
    }

    private static SiteStatsService CreateService(
        IHitCounter hitCounter,
        SqliteConnection connection)
    {
        var visitorIdProvider = new VisitorIdProvider(
            Options.Create(new TelemetryOptions { VisitorHashKey = "test-key" }),
            NullLogger<VisitorIdProvider>.Instance);

        return new SiteStatsService(hitCounter, visitorIdProvider, connection);
    }

    private sealed class StubHitCounter(
        HitCountStats? recordResult = null,
        HitCountStats? getResult = null) : IHitCounter
    {
        private readonly HitCountStats _recordResult =
            recordResult ?? new HitCountStats(0, 0);
        private readonly HitCountStats _getResult =
            getResult ?? new HitCountStats(0, 0);

        public int RecordCalls { get; private set; }
        public int GetCalls { get; private set; }

        public Task<HitCountStats> RecordHitAsync(
            string visitorKey,
            CancellationToken ct = default)
        {
            RecordCalls++;
            return Task.FromResult(_recordResult);
        }

        public Task<HitCountStats> GetHitCountsAsync(
            CancellationToken ct = default)
        {
            GetCalls++;
            return Task.FromResult(_getResult);
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path, SqliteConnection connection)
        {
            Path = path;
            Connection = connection;
        }

        private string Path { get; }
        public SqliteConnection Connection { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"randomgithub-tests-{Guid.NewGuid():N}.db");
            var connection = new SqliteConnection(
                $"Data Source={path};Pooling=False");

            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE AppStats (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    RandomRepositoriesServed INTEGER NOT NULL DEFAULT 0
                );
                """;
            await command.ExecuteNonQueryAsync();

            return new TestDatabase(path, connection);
        }

        public async Task<long> GetRandomRepositoriesServedAsync()
        {
            await using var command = Connection.CreateCommand();
            command.CommandText = """
                SELECT RandomRepositoriesServed
                FROM AppStats
                WHERE Id = 1;
                """;

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            File.Delete(Path);
        }
    }
}
