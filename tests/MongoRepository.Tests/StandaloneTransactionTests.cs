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
    public async Task ExecuteInTransactionAsync_OnStandalone_ThrowsFromDriver()
    {
        // Standalone Mongo does not support multi-document transactions. The
        // driver throws when StartTransaction is invoked. Capability detection
        // (SupportsTransactionsAsync) already returns false here; this test
        // proves that ignoring that signal still fails loudly rather than
        // silently running outside a transaction.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await _repo.ExecuteInTransactionAsync(async (session, ct) =>
            {
                await _repo.Add(new TestItem { Id = "standalone-tx", Name = "Alpha", Value = 1 }, session: session, cancellationToken: ct);
            });
        });
    }
}
