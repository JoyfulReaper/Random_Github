using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RandomGithub.Web.Options;
using RandomGithub.Web.Services;

namespace RandomGithub.Tests;

public sealed class VisitorIdProviderTests
{
    [Fact]
    public void GetVisitorId_SameIpv4AddressAndKey_ReturnsSameId()
    {
        var firstProvider = CreateProvider("shared-secret");
        var secondProvider = CreateProvider("shared-secret");

        var first = firstProvider.GetVisitorId("192.0.2.10");
        var second = secondProvider.GetVisitorId("192.0.2.10");

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetVisitorId_DifferentAddresses_ReturnsDifferentIds()
    {
        var provider = CreateProvider();

        var first = provider.GetVisitorId("192.0.2.10");
        var second = provider.GetVisitorId("192.0.2.11");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetVisitorId_Ipv4AndMappedIpv6_ReturnSameId()
    {
        var provider = CreateProvider();

        var ipv4 = provider.GetVisitorId("192.0.2.10");
        var mappedIpv6 = provider.GetVisitorId("::ffff:192.0.2.10");

        Assert.Equal(ipv4, mappedIpv6);
    }

    [Fact]
    public void GetVisitorId_MissingVisitorHashKey_ReturnsNull()
    {
        var provider = CreateProvider(null);

        var result = provider.GetVisitorId("192.0.2.10");

        Assert.Null(result);
    }

    [Fact]
    public void GetVisitorId_InvalidAddress_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var exception = Assert.Throws<ArgumentException>(
            () => provider.GetVisitorId("not-an-ip-address"));

        Assert.Equal("ipAddress", exception.ParamName);
    }

    [Fact]
    public void GetVisitorId_ValidAddress_ReturnsLowercaseSha256Hex()
    {
        var provider = CreateProvider();

        var result = provider.GetVisitorId("2001:db8::1");

        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        Assert.Matches("^[0-9a-f]{64}$", result);
    }

    private static VisitorIdProvider CreateProvider(string? visitorHashKey = "test-key")
    {
        var telemetryOptions = new TelemetryOptions();

        if (visitorHashKey is not null)
        {
            telemetryOptions.VisitorHashKey = visitorHashKey;
        }

        var options = Options.Create(telemetryOptions);

        return new VisitorIdProvider(
            options,
            NullLogger<VisitorIdProvider>.Instance);
    }
}
