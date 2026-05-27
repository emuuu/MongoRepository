using EphemeralMongo;
using Microsoft.Extensions.Options;

namespace MongoRepository.Tests.Infrastructure;

public class MongoDbReplicaSetFixture : IAsyncLifetime
{
    private IMongoRunner? _runner;

    public string ConnectionString { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        var options = new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = true,
            StandardOutputLogger = _ => { },
            StandardErrorLogger = _ => { }
        };
        _runner = MongoRunner.Run(options);
        ConnectionString = _runner.ConnectionString;
        return default;
    }

    public ValueTask DisposeAsync()
    {
        _runner?.Dispose();
        return default;
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
