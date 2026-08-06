using MongoDB.Bson;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

/// <summary>
/// Separates the two failure modes that used to collapse into the same result.
/// A key that cannot be serialised is a lookup that can never match, and still
/// yields null / an empty list. A document that cannot be deserialised is a
/// data problem and must surface as an exception instead of masquerading as
/// "not found".
/// </summary>
[Collection("MongoDB")]
public class KeyFormatAndSchemaDriftTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly ObjectIdKeyedItemRepository _objectIdRepo;
    private readonly DriftItemRepository _driftRepo;

    public KeyFormatAndSchemaDriftTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        var options = _fixture.CreateOptions();
        _objectIdRepo = new ObjectIdKeyedItemRepository(options);
        _driftRepo = new DriftItemRepository(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _objectIdRepo.Collection.Database.DropCollectionAsync("ObjectIdKeyedItems");
        await _driftRepo.Collection.Database.DropCollectionAsync("DriftItems");
    }

    public ValueTask DisposeAsync() => default;

    /// <summary>
    /// Writes a document straight through the BSON collection, bypassing the
    /// entity mapping — the only way to produce a stored shape the C# class can
    /// no longer read.
    /// </summary>
    private Task SeedDriftedDocument(string id) =>
        _driftRepo.Collection.Database
            .GetCollection<BsonDocument>("DriftItems")
            .InsertOneAsync(new BsonDocument
            {
                { "_id", id },
                { "Name", "Legacy" },
                // Was written as an int; the document now carries a string.
                { "Value", "not-a-number" }
            });

    // --- (a) Key that cannot be serialised: still null / empty ---

    [Fact]
    public async Task Get_ById_UnserialisableKey_ReturnsNull()
    {
        var result = await _objectIdRepo.Get("not-an-objectid");

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_ById_WellFormedKey_StillResolves()
    {
        var id = ObjectId.GenerateNewId().ToString();
        await _objectIdRepo.Add(new ObjectIdKeyedItem { Id = id, Name = "Alpha" });

        var result = await _objectIdRepo.Get(id);

        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task Get_ByIds_UnserialisableKey_ReturnsEmptyList()
    {
        var id = ObjectId.GenerateNewId().ToString();
        await _objectIdRepo.Add(new ObjectIdKeyedItem { Id = id, Name = "Alpha" });

        // One malformed key rejects the whole query, including the well-formed one.
        var result = await _objectIdRepo.Get(new[] { id, "not-an-objectid" });

        Assert.Empty(result);
    }

    // --- (b) Document that cannot be deserialised: must throw ---

    [Fact]
    public async Task Get_ById_SchemaDrift_Throws()
    {
        await SeedDriftedDocument("drift-1");

        var ex = await Assert.ThrowsAsync<FormatException>(() => _driftRepo.Get("drift-1"));

        Assert.Contains(nameof(DriftItem.Value), ex.Message);
    }

    [Fact]
    public async Task Get_ByIds_SchemaDrift_Throws()
    {
        await SeedDriftedDocument("drift-1");

        var ex = await Assert.ThrowsAsync<FormatException>(() => _driftRepo.Get(new[] { "drift-1" }));

        Assert.Contains(nameof(DriftItem.Value), ex.Message);
    }

    [Fact]
    public async Task Get_ById_SchemaDrift_DocumentIsActuallyPresent()
    {
        await SeedDriftedDocument("drift-1");

        // Guards the premise of the tests above: the read fails because the
        // document cannot be mapped, not because it is missing.
        var count = await _driftRepo.Collection.Database
            .GetCollection<BsonDocument>("DriftItems")
            .CountDocumentsAsync(new BsonDocument("_id", "drift-1"));

        Assert.Equal(1, count);
    }
}
