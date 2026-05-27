using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB-RS")]
public class TransactionTests : IAsyncLifetime
{
    private readonly MongoDbReplicaSetFixture _fixture;
    private readonly TestReadWriteRepository _repo;

    public TransactionTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
        _repo = new TestReadWriteRepository(_fixture.CreateOptions());
    }

    public async ValueTask InitializeAsync()
    {
        await _repo.Collection.Database.DropCollectionAsync("TestItems");
    }

    public ValueTask DisposeAsync() => default;

    [Fact]
    public async Task SupportsTransactionsAsync_OnReplicaSet_ReturnsTrue()
    {
        Assert.True(await _repo.SupportsTransactionsAsync());
    }

    [Fact]
    public async Task StartSessionAsync_ReturnsHandleBoundToRepositoryClient()
    {
        using var session = await _repo.StartSessionAsync();

        // Sanity: the session works against the repo's collection. If the session
        // came from a different client, InsertOneAsync(session, ...) would throw
        // InvalidOperationException("Session was not created by this client").
        var item = new TestItem { Id = "bound", Name = "Alpha", Value = 1 };
        await _repo.Collection.InsertOneAsync(session, item);

        var fetched = await _repo.Get("bound");
        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OnSuccess_Commits()
    {
        await _repo.ExecuteInTransactionAsync(async (session, ct) =>
        {
            await _repo.Add(new TestItem { Id = "tx-commit", Name = "Alpha", Value = 1 }, session: session, cancellationToken: ct);
            await _repo.Add(new TestItem { Id = "tx-commit-2", Name = "Beta", Value = 2 }, session: session, cancellationToken: ct);
        });

        var all = await _repo.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OnException_Aborts()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _repo.ExecuteInTransactionAsync(async (session, ct) =>
            {
                await _repo.Add(new TestItem { Id = "tx-abort", Name = "Alpha", Value = 1 }, session: session, cancellationToken: ct);
                throw new InvalidOperationException("boom");
            });
        });

        var all = await _repo.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ReadInsideTx_SeesOwnWrites()
    {
        await _repo.ExecuteInTransactionAsync(async (session, ct) =>
        {
            await _repo.Add(new TestItem { Id = "tx-read", Name = "Alpha", Value = 42 }, session: session, cancellationToken: ct);

            var fetched = await _repo.Get("tx-read", session: session, cancellationToken: ct);
            Assert.NotNull(fetched);
            Assert.Equal(42, fetched.Value);
        });
    }

    [Fact]
    public async Task Add_WithSession_ParticipatesInTransaction()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Add(new TestItem { Id = "tx-participate", Name = "Alpha", Value = 1 }, session: session);

        // Outside-of-session read should NOT see uncommitted insert
        var beforeCommit = await _repo.Get("tx-participate");
        Assert.Null(beforeCommit);

        await session.AbortTransactionAsync();

        var afterAbort = await _repo.Get("tx-participate");
        Assert.Null(afterAbort);
    }
}
