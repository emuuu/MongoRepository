using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoRepository.HealthChecks;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class HealthCheckBuilderExtensionsTests
{
    private readonly MongoDbFixture _fixture;

    public HealthCheckBuilderExtensionsTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    private IServiceCollection CreateServicesWithMongo()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<MongoDbOptions>>(sp => _fixture.CreateOptions());
        services.AddHealthChecks();
        return services;
    }

    [Fact]
    public void AddMongoRepository_Default_RegistersCheck()
    {
        var services = CreateServicesWithMongo();

        services.AddHealthChecks().AddMongoRepository();

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.Contains(options.Value.Registrations, r => r.Name == "mongo_repository");
    }

    [Fact]
    public void AddMongoRepository_WithConfigure_AppliesOptions()
    {
        var services = CreateServicesWithMongo();

        services.AddHealthChecks().AddMongoRepository(opts =>
        {
            opts.SingleFailureIsUnhealthy = true;
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.Contains(options.Value.Registrations, r => r.Name == "mongo_repository");
    }

    [Fact]
    public void AddMongoRepository_NullConfigure_DoesNotThrow()
    {
        var services = CreateServicesWithMongo();

        services.AddHealthChecks().AddMongoRepository(configure: null!);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.Contains(options.Value.Registrations, r => r.Name == "mongo_repository");
    }

    [Fact]
    public void AddMongoRepository_CustomName_UsesName()
    {
        var services = CreateServicesWithMongo();

        services.AddHealthChecks().AddMongoRepository(name: "custom_mongo");

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.Contains(options.Value.Registrations, r => r.Name == "custom_mongo");
    }
}
