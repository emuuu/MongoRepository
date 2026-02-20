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

    public Task InitializeAsync()
    {
        return _repo.Collection.Database.DropCollectionAsync("TestItems");
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
