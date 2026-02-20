using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoRepository.HealthChecks;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class MongoRepositoryHealthCheckTests
{
    private readonly MongoDbFixture _fixture;

    public MongoRepositoryHealthCheckTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    private static IOptions<MongoDbOptions> FailingOptions() =>
        Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = "mongodb://localhost:1/?serverSelectionTimeout=1s&connectTimeout=1s",
            ReadWriteConnection = "mongodb://localhost:1/?serverSelectionTimeout=1s&connectTimeout=1s"
        });

    private static IOptions<MongoDbOptions> EmptyOptions() =>
        Options.Create(new MongoDbOptions());

    // --- Constructor ---

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MongoRepositoryHealthCheck(null!));
    }

    [Fact]
    public void Constructor_NullBehavior_UsesDefaults()
    {
        var options = _fixture.CreateOptions();
        var check = new MongoRepositoryHealthCheck(options, behavior: null);

        Assert.NotNull(check);
    }

    // --- Both connections OK ---

    [Fact]
    public async Task BothConnectionsOk_ReturnsHealthy()
    {
        var check = new MongoRepositoryHealthCheck(_fixture.CreateOptions());

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("ok", result.Data["readOnly"]);
        Assert.Equal("ok", result.Data["readWrite"]);
    }

    // --- Both missing ---

    [Fact]
    public async Task BothMissing_DefaultBehavior_ReturnsHealthy()
    {
        var check = new MongoRepositoryHealthCheck(EmptyOptions());

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("skipped", result.Data["readOnly"]);
        Assert.Equal("skipped", result.Data["readWrite"]);
    }

    [Fact]
    public async Task BothMissing_MissingIsFailure_ReturnsUnhealthy()
    {
        var behavior = new MongoRepositoryHealthCheckOptions { MissingConnectionIsFailure = true };
        var check = new MongoRepositoryHealthCheck(EmptyOptions(), behavior);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.IsType<AggregateException>(result.Exception);
    }

    // --- ReadOnly missing, ReadWrite OK ---

    [Fact]
    public async Task ReadOnlyMissing_ReadWriteOk_ReturnsHealthy()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = null,
            ReadWriteConnection = _fixture.ConnectionString
        });
        var check = new MongoRepositoryHealthCheck(options);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("skipped", result.Data["readOnly"]);
        Assert.Equal("ok", result.Data["readWrite"]);
    }

    [Fact]
    public async Task ReadOnlyMissing_MissingIsFailure_ReturnsDegraded()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = null,
            ReadWriteConnection = _fixture.ConnectionString
        });
        var behavior = new MongoRepositoryHealthCheckOptions { MissingConnectionIsFailure = true };
        var check = new MongoRepositoryHealthCheck(options, behavior);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task ReadOnlyMissing_MissingIsFailure_SingleUnhealthy_ReturnsUnhealthy()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = null,
            ReadWriteConnection = _fixture.ConnectionString
        });
        var behavior = new MongoRepositoryHealthCheckOptions
        {
            MissingConnectionIsFailure = true,
            SingleFailureIsUnhealthy = true
        };
        var check = new MongoRepositoryHealthCheck(options, behavior);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    // --- ReadOnly fails, ReadWrite OK ---

    [Fact]
    public async Task ReadOnlyFails_ReadWriteOk_ReturnsDegraded()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = "mongodb://localhost:1/?serverSelectionTimeout=1s&connectTimeout=1s",
            ReadWriteConnection = _fixture.ConnectionString
        });
        var check = new MongoRepositoryHealthCheck(options);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task ReadOnlyFails_SingleUnhealthy_ReturnsUnhealthy()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = "mongodb://localhost:1/?serverSelectionTimeout=1s&connectTimeout=1s",
            ReadWriteConnection = _fixture.ConnectionString
        });
        var behavior = new MongoRepositoryHealthCheckOptions { SingleFailureIsUnhealthy = true };
        var check = new MongoRepositoryHealthCheck(options, behavior);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    // --- Both fail ---

    [Fact]
    public async Task BothFail_ReturnsUnhealthy_WithAggregateException()
    {
        var check = new MongoRepositoryHealthCheck(FailingOptions());

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.IsType<AggregateException>(result.Exception);
    }

    // --- Data dictionary ---

    [Fact]
    public async Task SkippedConnection_DataShowsSkipped()
    {
        var options = Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = null,
            ReadWriteConnection = _fixture.ConnectionString
        });
        var check = new MongoRepositoryHealthCheck(options);

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.True((bool)result.Data["readOnlySkipped"]);
    }
}
