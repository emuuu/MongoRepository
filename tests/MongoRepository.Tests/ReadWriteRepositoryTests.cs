using MongoDB.Bson;
using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class ReadWriteRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly TestReadWriteRepository _repo;

    public ReadWriteRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repo = new TestReadWriteRepository(_fixture.CreateOptions());
    }

    public async ValueTask InitializeAsync()
    {
        await _repo.Collection.Database.DropCollectionAsync("TestItems");
    }

    public ValueTask DisposeAsync() => default;

    // --- TrimStrings via Add ---

    [Fact]
    public async Task Add_TrimsStringProperties()
    {
        var item = new TestItem { Id = "1", Name = "  Alpha  ", Description = "  Desc  ", Value = 10 };

        await _repo.Add(item);
        var result = await _repo.Get("1");

        Assert.Equal("Alpha", result!.Name);
        Assert.Equal("Desc", result.Description);
    }

    [Fact]
    public async Task Add_SkipsBsonIgnoreProperties()
    {
        var item = new TestItem { Id = "1", Name = "Alpha", IgnoredProperty = "  NotTrimmed  ", Value = 10 };

        await _repo.Add(item);

        // IgnoredProperty is BsonIgnore — TrimStrings should skip it
        // The property still retains its original value in-memory (not trimmed)
        Assert.Equal("  NotTrimmed  ", item.IgnoredProperty);
    }

    [Fact]
    public async Task Add_NullString_DoesNotThrow()
    {
        var item = new TestItem { Id = "1", Name = null, Description = null, Value = 10 };

        await _repo.Add(item);
        var result = await _repo.Get("1");

        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task Add_WhitespaceOnlyString_IsNotTrimmed()
    {
        var item = new TestItem { Id = "1", Name = "   ", Value = 10 };

        await _repo.Add(item);
        var result = await _repo.Get("1");

        // IsNullOrWhiteSpace check means whitespace-only strings are NOT trimmed
        Assert.Equal("   ", result!.Name);
    }

    [Fact]
    public async Task Add_DuplicateKey_ThrowsException()
    {
        var item = new TestItem { Id = "dup", Name = "First", Value = 1 };
        await _repo.Add(item);

        var duplicate = new TestItem { Id = "dup", Name = "Second", Value = 2 };
        await Assert.ThrowsAsync<MongoWriteException>(() => _repo.Add(duplicate));
    }

    // --- The key is never trimmed ---

    private IMongoCollection<BsonDocument> RawItems =>
        _repo.Collection.Database.GetCollection<BsonDocument>("TestItems");

    [Fact]
    public async Task Add_IdWithSurroundingWhitespace_IsStoredVerbatim()
    {
        const string id = "  event-1: Main Street 1  ";
        var item = new TestItem { Id = id, Name = "  Alpha  ", Value = 10 };

        await _repo.Add(item);

        Assert.Equal(id, item.Id);
        var stored = await RawItems.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleOrDefaultAsync();
        Assert.NotNull(stored);
        var result = await _repo.Get(id);
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        // Everything but the key is still trimmed.
        Assert.Equal("Alpha", result.Name);
    }

    // A natural key built from untrimmed input: the second Add of the same key
    // fails with DuplicateKey, and the re-lookup with that very key has to find
    // the document the first Add wrote.
    [Fact]
    public async Task Add_DuplicateIdWithSurroundingWhitespace_ThrowsAndGetFindsExisting()
    {
        const string id = " event-1: Main Street 1 ";
        await _repo.Add(new TestItem { Id = id, Name = "First", Value = 1 });

        var ex = await Assert.ThrowsAsync<MongoWriteException>(
            () => _repo.Add(new TestItem { Id = id, Name = "Second", Value = 2 }));
        Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError?.Category);

        var existing = await _repo.Get(id);
        Assert.NotNull(existing);
        Assert.Equal("First", existing.Name);
    }

    [Fact]
    public async Task Update_Single_IdWithSurroundingWhitespace_MatchesExistingDocument()
    {
        const string id = " key-1 ";
        await RawItems.InsertOneAsync(new BsonDocument { { "_id", id }, { "Name", "Alpha" }, { "Value", 1 } });

        var result = await _repo.Update(
            new TestItem { Id = id, Name = "  Updated  ", Value = 2 },
            new ReplaceOptions { IsUpsert = true });

        Assert.Equal(1, result.MatchedCount);
        Assert.Null(result.UpsertedId);
        Assert.Equal(1, await RawItems.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty));
        var fetched = await _repo.Get(id);
        Assert.Equal("Updated", fetched!.Name);
        Assert.Equal(2, fetched.Value);
    }

    [Fact]
    public async Task AddRange_IdsWithSurroundingWhitespace_AreStoredVerbatim()
    {
        var items = new[]
        {
            new TestItem { Id = " a ", Name = "  Alpha  ", Value = 10 },
            new TestItem { Id = "b  ", Name = "  Beta  ", Value = 20 }
        };

        await _repo.AddRange(items);

        Assert.Equal(" a ", items[0].Id);
        Assert.Equal("b  ", items[1].Id);
        var found = await _repo.Get(new[] { " a ", "b  " });
        Assert.Equal(2, found.Count);
        Assert.All(found, item => Assert.DoesNotContain(" ", item.Name!));
    }

    [Fact]
    public async Task Update_Bulk_IdsWithSurroundingWhitespace_MatchExistingDocuments()
    {
        await RawItems.InsertManyAsync(new[]
        {
            new BsonDocument { { "_id", " a " }, { "Name", "Alpha" }, { "Value", 1 } },
            new BsonDocument { { "_id", "b  " }, { "Name", "Beta" }, { "Value", 2 } }
        });

        var result = await _repo.Update(new[]
        {
            new TestItem { Id = " a ", Name = "  X  ", Value = 10 },
            new TestItem { Id = "b  ", Name = "  Y  ", Value = 20 }
        });

        Assert.Equal(2, result.MatchedCount);
        Assert.Equal(2, await RawItems.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty));
        Assert.Equal("X", (await _repo.Get(" a "))!.Name);
        Assert.Equal("Y", (await _repo.Get("b  "))!.Name);
    }

    [Fact]
    public async Task Add_BsonIdOnDifferentlyNamedMember_IsStoredVerbatim()
    {
        var repo = new AlternateKeyItemRepository(_fixture.CreateOptions());
        await repo.Collection.Database.DropCollectionAsync("AlternateKeyItems");

        await repo.Add(new AlternateKeyItem { Key = " k-1 ", Id = " k-1 ", Name = "  Alpha  " });

        var stored = await repo.Collection.Database.GetCollection<BsonDocument>("AlternateKeyItems")
            .Find(Builders<BsonDocument>.Filter.Empty).SingleAsync();
        Assert.Equal(" k-1 ", stored["_id"].AsString);
        Assert.Equal("Alpha", stored["Name"].AsString);
    }

    // --- [NoTrim] opts a property out ---

    [Fact]
    public async Task Add_NoTrimProperty_IsStoredVerbatim()
    {
        var repo = new NoTrimItemRepository(_fixture.CreateOptions());
        await repo.Collection.Database.DropCollectionAsync("NoTrimItems");
        var item = new NoTrimItem { Id = "1", Name = "  Alpha  ", Signature = "  sig  " };

        await repo.Add(item);

        Assert.Equal("  sig  ", item.Signature);
        var result = await repo.Get("1");
        Assert.Equal("  sig  ", result!.Signature);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task Update_Single_NoTrimProperty_IsStoredVerbatim()
    {
        var repo = new NoTrimItemRepository(_fixture.CreateOptions());
        await repo.Collection.Database.DropCollectionAsync("NoTrimItems");
        await repo.Add(new NoTrimItem { Id = "1", Name = "Alpha", Signature = "sig" });

        await repo.Update(new NoTrimItem { Id = "1", Name = "  Beta  ", Signature = " sig2 " });

        var result = await repo.Get("1");
        Assert.Equal(" sig2 ", result!.Signature);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public async Task Add_StaticStringProperty_IsNotTrimmed()
    {
        var repo = new StaticPropertyItemRepository(_fixture.CreateOptions());
        await repo.Collection.Database.DropCollectionAsync("StaticPropertyItems");
        StaticPropertyItem.Shared = "  shared  ";

        await repo.Add(new StaticPropertyItem { Id = "1", Name = "  Alpha  " });

        Assert.Equal("  shared  ", StaticPropertyItem.Shared);
        Assert.Equal("Alpha", (await repo.Get("1"))!.Name);
    }

    // --- Each entity sequence is enumerated once ---

    [Fact]
    public async Task AddRange_LazySequence_InsertsTheTrimmedInstances()
    {
        var source = new[] { ("1", "  Alpha  "), ("2", "  Beta  ") };

        await _repo.AddRange(source.Select(s => new TestItem { Id = s.Item1, Name = s.Item2 }));

        var all = await _repo.GetAll();
        Assert.Equal(new[] { "Alpha", "Beta" }, all.Select(x => x.Name));
    }

    [Fact]
    public async Task Update_Bulk_LazySequence_IsEnumeratedOnce()
    {
        var items = new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        };
        await _repo.AddRange(items);

        await _repo.Update(items.Select(i => { i.Value *= 2; return i; }));

        var all = await _repo.GetAll();
        Assert.Equal(new[] { 20, 40 }, all.Select(x => x.Value));
    }

    // Regression: Add() must surface MongoWriteException for sparse compound
    // unique index violations, not only for the default _id unique index.
    [Fact]
    public async Task Add_SparseCompoundUniqueViolation_ThrowsMongoWriteException()
    {
        var repo = new OriginMarkerItemRepository(_fixture.CreateOptions());
        await repo.Collection.Database.DropCollectionAsync("OriginMarkerItems");

        var indexKeys = Builders<OriginMarkerItem>.IndexKeys
            .Ascending(x => x.OriginEventId)
            .Ascending(x => x.OriginDiscriminator);
        var indexOptions = new CreateIndexOptions { Unique = true, Sparse = true };
        await repo.Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<OriginMarkerItem>(indexKeys, indexOptions));

        var first = new OriginMarkerItem
        {
            Id = ObjectId.GenerateNewId().ToString(),
            OriginEventId = "event-1",
            OriginDiscriminator = "item-1"
        };
        await repo.Add(first);

        var collision = new OriginMarkerItem
        {
            Id = ObjectId.GenerateNewId().ToString(),
            OriginEventId = "event-1",
            OriginDiscriminator = "item-1"
        };

        var ex = await Assert.ThrowsAsync<MongoWriteException>(() => repo.Add(collision));
        Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError?.Category);

        var count = await repo.Collection.CountDocumentsAsync(Builders<OriginMarkerItem>.Filter.Empty);
        Assert.Equal(1, count);
    }

    // --- CRUD: Add ---

    [Fact]
    public async Task Add_InsertsEntity()
    {
        var item = new TestItem { Id = "1", Name = "Alpha", Value = 10 };

        await _repo.Add(item);

        var result = await _repo.Get("1");
        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task AddRange_InsertsMultiple()
    {
        var items = new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        };

        await _repo.AddRange(items);

        var all = await _repo.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task AddRange_TrimsAllEntities()
    {
        var items = new[]
        {
            new TestItem { Id = "1", Name = "  Alpha  ", Value = 10 },
            new TestItem { Id = "2", Name = "  Beta  ", Value = 20 }
        };

        await _repo.AddRange(items);

        var all = await _repo.GetAll();
        Assert.All(all, item => Assert.DoesNotContain(" ", item.Name!));
    }

    // --- CRUD: Update ---

    [Fact]
    public async Task Update_Single_ReplacesEntity()
    {
        await _repo.Add(new TestItem { Id = "1", Name = "Alpha", Value = 10 });

        var updated = new TestItem { Id = "1", Name = "Updated", Value = 99 };
        var result = await _repo.Update(updated);

        Assert.Equal(1, result.ModifiedCount);
        var fetched = await _repo.Get("1");
        Assert.Equal("Updated", fetched!.Name);
        Assert.Equal(99, fetched.Value);
    }

    [Fact]
    public async Task Update_Single_TrimsStrings()
    {
        await _repo.Add(new TestItem { Id = "1", Name = "Alpha", Value = 10 });

        var updated = new TestItem { Id = "1", Name = "  Trimmed  ", Value = 10 };
        await _repo.Update(updated);

        var fetched = await _repo.Get("1");
        Assert.Equal("Trimmed", fetched!.Name);
    }

    [Fact]
    public async Task Update_Single_NonExistent_ReturnsZeroModified()
    {
        var item = new TestItem { Id = "nonexistent", Name = "Ghost", Value = 0 };
        var result = await _repo.Update(item);

        Assert.Equal(0, result.ModifiedCount);
    }

    [Fact]
    public async Task Update_Bulk_ReplacesMultiple()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        });

        var updates = new[]
        {
            new TestItem { Id = "1", Name = "Alpha2", Value = 11 },
            new TestItem { Id = "2", Name = "Beta2", Value = 22 }
        };
        var result = await _repo.Update(updates);

        Assert.Equal(2, result.ModifiedCount);
    }

    [Fact]
    public async Task Update_Bulk_TrimsStrings()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        });

        var updates = new[]
        {
            new TestItem { Id = "1", Name = "  X  ", Value = 10 },
            new TestItem { Id = "2", Name = "  Y  ", Value = 20 }
        };
        await _repo.Update(updates);

        var all = await _repo.GetAll();
        Assert.Contains(all, x => x.Name == "X");
        Assert.Contains(all, x => x.Name == "Y");
    }

    // --- CRUD: Delete ---

    [Fact]
    public async Task Delete_ById_RemovesEntity()
    {
        await _repo.Add(new TestItem { Id = "1", Name = "Alpha", Value = 10 });

        var result = await _repo.Delete("1");

        Assert.Equal(1, result.DeletedCount);
        Assert.Null(await _repo.Get("1"));
    }

    [Fact]
    public async Task Delete_ById_NonExistent_ReturnsZeroDeleted()
    {
        var result = await _repo.Delete("nonexistent");

        Assert.Equal(0, result.DeletedCount);
    }

    [Fact]
    public async Task Delete_ByIds_RemovesMultiple()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 },
            new TestItem { Id = "3", Name = "Charlie", Value = 30 }
        });

        var result = await _repo.Delete(new[] { "1", "3" });

        Assert.Equal(2, result.DeletedCount);
        var remaining = await _repo.GetAll();
        Assert.Single(remaining);
        Assert.Equal("Beta", remaining[0].Name);
    }

    [Fact]
    public async Task Delete_ByFilter_RemovesMatching()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        });

        var filter = Builders<TestItem>.Filter.Eq(x => x.Name, "Alpha");
        var result = await _repo.Delete(filter);

        Assert.Equal(1, result.DeletedCount);
    }

    [Fact]
    public async Task Delete_ByFilter_NullFilter_RemovesAll()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 }
        });

        var result = await _repo.Delete(filterDefinition: null);

        Assert.Equal(2, result.DeletedCount);
    }

    [Fact]
    public async Task Delete_ByExpression_RemovesMatching()
    {
        await _repo.AddRange(new[]
        {
            new TestItem { Id = "1", Name = "Alpha", Value = 10 },
            new TestItem { Id = "2", Name = "Beta", Value = 20 },
            new TestItem { Id = "3", Name = "Charlie", Value = 30 }
        });

        var result = await _repo.Delete(x => x.Value >= 20);

        Assert.Equal(2, result.DeletedCount);
        var remaining = await _repo.GetAll();
        Assert.Single(remaining);
    }
}
