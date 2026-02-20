using EphemeralMongo;
using Microsoft.Extensions.Options;
using MongoRepository;

namespace MongoRepository.Tests.Infrastructure;

public class MongoDbFixture : IAsyncLifetime
{
    private IMongoRunner? _runner;

    public string ConnectionString { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var options = new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = false,
            StandardOutputLogger = _ => { },
            StandardErrorLogger = _ => { }
        };
        _runner = MongoRunner.Run(options);
        ConnectionString = _runner.ConnectionString;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _runner?.Dispose();
        return Task.CompletedTask;
    }

    public IOptions<MongoDbOptions> CreateOptions()
    {
        return Options.Create(new MongoDbOptions
        {
            ReadOnlyConnection = ConnectionString,
            ReadWriteConnection = ConnectionString
        });
    }
}
