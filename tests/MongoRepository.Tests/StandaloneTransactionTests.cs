using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class StandaloneTransactionTests
{
    private readonly MongoDbFixture _fixture;
    private readonly TestReadWriteRepository _repo;

    public StandaloneTransactionTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repo = new TestReadWriteRepository(_fixture.CreateOptions());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OnStandalone_ThrowsNotSupported()
    {
        // Force cluster discovery so the driver knows the topology is Standalone
        // before StartTransaction. Without the prior probe the driver may attempt
        // a retryable write and surface a MongoCommandException instead.
        Assert.False(await _repo.SupportsTransactionsAsync());

        // Standalone Mongo does not support transactions; the driver's
        // EnsureTransactionsAreSupported throws on StartTransaction. Confirm the
        // wrapper surfaces it loudly rather than degrading silently to a
        // non-transactional run.
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await _repo.ExecuteInTransactionAsync(async (session, ct) =>
            {
                await _repo.Add(new TestItem { Id = "standalone-tx", Name = "Alpha", Value = 1 }, session: session, cancellationToken: ct);
            });
        });
    }
}
