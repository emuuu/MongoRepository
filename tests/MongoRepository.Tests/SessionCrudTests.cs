using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

// Exercises every session-aware overload of IReadWriteRepository /
// IReadOnlyDataRepository to verify the optional IClientSessionHandle parameter
// is wired through to the driver's session-accepting collection methods.
// Each test runs the operation inside an open transaction on a single-node
// replica set; aborting the transaction proves the call honoured the session
// (otherwise the document would be visible after rollback).
[Collection("MongoDB-RS")]
public class SessionCrudTests : IAsyncLifetime
{
    private readonly MongoDbReplicaSetFixture _fixture;
    private readonly TestReadWriteRepository _repo;

    public SessionCrudTests(MongoDbReplicaSetFixture fixture)
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
    public async Task AddRange_WithSession_AbortLeavesNoDocuments()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.AddRange(new[]
        {
            new TestItem { Id = "ar-1", Name = "A", Value = 1 },
            new TestItem { Id = "ar-2", Name = "B", Value = 2 }
        }, session: session);

        await session.AbortTransactionAsync();

        Assert.Empty(await _repo.GetAll());
    }

    [Fact]
    public async Task Update_Single_WithSession_AbortReverts()
    {
        await _repo.Add(new TestItem { Id = "u1", Name = "Original", Value = 1 });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Update(new TestItem { Id = "u1", Name = "Modified", Value = 2 }, session: session);

        await session.AbortTransactionAsync();

        var fetched = await _repo.Get("u1");
        Assert.Equal("Original", fetched!.Name);
    }

    [Fact]
    public async Task Update_Bulk_WithSession_AbortReverts()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "ub1", Name = "A", Value = 1 },
            new TestItem { Id = "ub2", Name = "B", Value = 2 }
        });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Update(new[]
        {
            new TestItem { Id = "ub1", Name = "A2", Value = 10 },
            new TestItem { Id = "ub2", Name = "B2", Value = 20 }
        }, session: session);

        await session.AbortTransactionAsync();

        var fetched = await _repo.Get(new[] { "ub1", "ub2" });
        Assert.Contains(fetched, x => x.Name == "A" && x.Value == 1);
        Assert.Contains(fetched, x => x.Name == "B" && x.Value == 2);
    }

    [Fact]
    public async Task Delete_ById_WithSession_AbortPreservesDocument()
    {
        await _repo.Add(new TestItem { Id = "d1", Name = "A", Value = 1 });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Delete("d1", session: session);

        await session.AbortTransactionAsync();

        Assert.NotNull(await _repo.Get("d1"));
    }

    [Fact]
    public async Task Delete_ByIds_WithSession_AbortPreservesDocuments()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "dm1", Name = "A", Value = 1 },
            new TestItem { Id = "dm2", Name = "B", Value = 2 }
        });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Delete(new[] { "dm1", "dm2" }, session: session);

        await session.AbortTransactionAsync();

        Assert.Equal(2, (await _repo.GetAll()).Count);
    }

    [Fact]
    public async Task Delete_ByFilter_WithSession_AbortPreservesDocuments()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "df1", Name = "Alpha", Value = 1 },
            new TestItem { Id = "df2", Name = "Beta", Value = 2 }
        });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        var filter = Builders<TestItem>.Filter.Eq(x => x.Name, "Alpha");
        await _repo.Delete(filter, session: session);

        await session.AbortTransactionAsync();

        Assert.NotNull(await _repo.Get("df1"));
    }

    [Fact]
    public async Task Delete_ByExpression_WithSession_AbortPreservesDocuments()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "de1", Name = "A", Value = 10 },
            new TestItem { Id = "de2", Name = "B", Value = 20 }
        });

        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Delete(x => x.Value >= 10, session: session);

        await session.AbortTransactionAsync();

        Assert.Equal(2, (await _repo.GetAll()).Count);
    }

    [Fact]
    public async Task Get_ByIds_WithSession_SeesUncommittedInsert()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Add(new TestItem { Id = "g1", Name = "A", Value = 1 }, session: session);
        await _repo.Add(new TestItem { Id = "g2", Name = "B", Value = 2 }, session: session);

        var insideTx = await _repo.Get(new[] { "g1", "g2" }, session: session);
        Assert.Equal(2, insideTx.Count);

        var outsideTx = await _repo.Get(new[] { "g1", "g2" });
        Assert.Empty(outsideTx);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task Get_ByFilter_WithSession_SeesUncommittedInsert()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Add(new TestItem { Id = "gf1", Name = "Findable", Value = 1 }, session: session);

        var filter = Builders<TestItem>.Filter.Eq(x => x.Name, "Findable");
        var insideTx = await _repo.Get(filter, session: session);
        Assert.NotNull(insideTx);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task GetAll_WithSession_SeesUncommittedInserts()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Add(new TestItem { Id = "ga1", Name = "A", Value = 1 }, session: session);
        await _repo.Add(new TestItem { Id = "ga2", Name = "B", Value = 2 }, session: session);

        var insideTx = await _repo.GetAll(session: session);
        Assert.Equal(2, insideTx.Count);

        var outsideTx = await _repo.GetAll();
        Assert.Empty(outsideTx);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task GetAll_FilterSortPaged_WithSession_SeesUncommittedInserts()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.AddRange(new[]
        {
            new TestItem { Id = "gp1", Name = "A", Value = 1 },
            new TestItem { Id = "gp2", Name = "B", Value = 2 },
            new TestItem { Id = "gp3", Name = "C", Value = 3 }
        }, session: session);

        var sort = Builders<TestItem>.Sort.Descending(x => x.Value);
        var page1 = await _repo.GetAll(filterDefinition: null, sortDefinition: sort, page: 1, pageSize: 2, session: session);
        Assert.Equal(2, page1.Count);
        Assert.Equal(3, page1[0].Value);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task GetAll_JsonFilter_WithSession_SeesUncommittedInserts()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.AddRange(new[]
        {
            new TestItem { Id = "gj1", Name = "A", Value = 10 },
            new TestItem { Id = "gj2", Name = "B", Value = 20 }
        }, session: session);

        var insideTx = await _repo.GetAll("{ Value: { $gte: 15 } }", session: session);
        Assert.Single(insideTx);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task Count_Filter_WithSession_SeesUncommittedInserts()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.Add(new TestItem { Id = "c1", Name = "A", Value = 1 }, session: session);

        var insideTx = await _repo.Count(filterDefinition: null, session: session);
        Assert.Equal(1, insideTx);

        var outsideTx = await _repo.Count();
        Assert.Equal(0, outsideTx);

        await session.AbortTransactionAsync();
    }

    [Fact]
    public async Task Count_JsonFilter_WithSession_SeesUncommittedInserts()
    {
        using var session = await _repo.StartSessionAsync();
        session.StartTransaction();

        await _repo.AddRange(new[]
        {
            new TestItem { Id = "cj1", Name = "A", Value = 5 },
            new TestItem { Id = "cj2", Name = "B", Value = 15 }
        }, session: session);

        var insideTx = await _repo.Count("{ Value: { $gte: 10 } }", session: session);
        Assert.Equal(1, insideTx);

        await session.AbortTransactionAsync();
    }
}
