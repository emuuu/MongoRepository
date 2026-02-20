using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class ReadOnlyDataRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly TestReadWriteRepository _writeRepo;
    private readonly TestReadOnlyRepository _readRepo;

    public ReadOnlyDataRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        var options = _fixture.CreateOptions();
        _writeRepo = new TestReadWriteRepository(options);
        _readRepo = new TestReadOnlyRepository(options);
    }

    public Task InitializeAsync()
    {
        // Clean up collection before each test
        return _writeRepo.Collection.Database.DropCollectionAsync("TestItems");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private TestItem CreateItem(string id, string name, int value) =>
        new() { Id = id, Name = name, Value = value };

    private async Task SeedItems(params TestItem[] items)
    {
        foreach (var item in items)
            await _writeRepo.Add(item);
    }

    // --- Get by Id ---

    [Fact]
    public async Task Get_ById_Existing_ReturnsEntity()
    {
        await SeedItems(CreateItem("1", "Alpha", 10));

        var result = await _readRepo.Get("1");

        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task Get_ById_NonExistent_ReturnsNull()
    {
        var result = await _readRepo.Get("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_ById_InvalidFormat_ReturnsNull()
    {
        // When using ObjectId-based lookups, invalid formats would throw FormatException
        // With string IDs, this just returns null for a non-existent key
        var result = await _readRepo.Get("");

        Assert.Null(result);
    }

    // --- Get by Ids ---

    [Fact]
    public async Task Get_ByIds_ReturnsMatching()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Gamma", 30));

        var result = await _readRepo.Get(new[] { "1", "3" });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
    }

    [Fact]
    public async Task Get_ByIds_EmptyList_ReturnsEmptyList()
    {
        await SeedItems(CreateItem("1", "Alpha", 10));

        var result = await _readRepo.Get(Array.Empty<string>());

        Assert.Empty(result);
    }

    // --- Get by Filter ---

    [Fact]
    public async Task Get_ByFilter_ReturnsFirstMatch()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var filter = Builders<TestItem>.Filter.Eq(x => x.Name, "Beta");
        var result = await _readRepo.Get(filter);

        Assert.NotNull(result);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public async Task Get_ByFilter_NullFilter_ReturnsFirst()
    {
        await SeedItems(CreateItem("1", "Alpha", 10));

        var result = await _readRepo.Get(filterDefinition: null);

        Assert.NotNull(result);
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_ReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var result = await _readRepo.GetAll();

        Assert.Equal(2, result.Count);
    }

    // --- GetAll with FilterDefinition ---

    [Fact]
    public async Task GetAll_FilterDefinition_WithPaging_ReturnsPage()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Gamma", 30));

        var filter = Builders<TestItem>.Filter.Empty;
        var result = await _readRepo.GetAll(filter, page: 1, pageSize: 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_FilterDefinition_PageLessThan1_ClampsTo1()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var filter = Builders<TestItem>.Filter.Empty;
        var result = await _readRepo.GetAll(filter, page: -5, pageSize: 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_FilterDefinition_NoPaging_ReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Gamma", 30));

        var filter = Builders<TestItem>.Filter.Eq(x => x.Value, 20);
        var result = await _readRepo.GetAll(filter);

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public async Task GetAll_FilterDefinition_NullFilter_ReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var result = await _readRepo.GetAll(filterDefinition: null, sortDefinition: null);

        Assert.Equal(2, result.Count);
    }

    // --- GetAll with JSON filter ---

    [Fact]
    public async Task GetAll_JsonFilter_ReturnsFiltered()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var result = await _readRepo.GetAll("{ \"Name\": \"Alpha\" }");

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public async Task GetAll_JsonFilter_NullOrEmpty_ReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var resultNull = await _readRepo.GetAll(jsonFilterDefinition: null);
        var resultEmpty = await _readRepo.GetAll(jsonFilterDefinition: "");

        Assert.Equal(2, resultNull.Count);
        Assert.Equal(2, resultEmpty.Count);
    }

    // --- GetAll with sorting expression ---

    [Fact]
    public async Task GetAll_Sorting_WithPaging_ReturnsSortedPage()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20));

        var result = await _readRepo.GetAll<string>(x => x.Name!, page: 1, pageSize: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task GetAll_Sorting_NoPaging_ReturnsSorted()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20));

        var result = await _readRepo.GetAll<string>(x => x.Name!);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("Charlie", result[2].Name);
    }

    // --- GetAll with filter and sorting ---

    [Fact]
    public async Task GetAll_FilterAndSorting_WithPaging()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20),
            CreateItem("4", "Delta", 10));

        var result = await _readRepo.GetAll(
            x => x.Value == 10,
            (TestItem x) => x.Name!,
            page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Delta", result[1].Name);
    }

    [Fact]
    public async Task GetAll_FilterAndSorting_NoPaging()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20));

        var result = await _readRepo.GetAll(
            x => x.Value >= 20,
            (TestItem x) => x.Name!);

        Assert.Equal(2, result.Count);
        Assert.Equal("Beta", result[0].Name);
        Assert.Equal("Charlie", result[1].Name);
    }

    // --- GetAllDescending with sorting ---

    [Fact]
    public async Task GetAllDescending_Sorting_WithPaging()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending<string>(x => x.Name!, page: 1, pageSize: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task GetAllDescending_Sorting_NoPaging()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending<string>(x => x.Name!);

        Assert.Equal(3, result.Count);
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Alpha", result[2].Name);
    }

    // --- GetAllDescending with filter and sorting ---

    [Fact]
    public async Task GetAllDescending_FilterAndSorting_WithPaging()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending(
            x => x.Value >= 20,
            (TestItem x) => x.Name!,
            page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task GetAllDescending_FilterAndSorting_NoPaging()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending(
            x => x.Value >= 20,
            (TestItem x) => x.Name!);

        Assert.Equal(2, result.Count);
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    // --- Obsolete GetAll with LINQ filter ---

    [Fact]
    public async Task GetAll_Obsolete_LinqFilter_WithPaging()
    {
#pragma warning disable CS0618 // Obsolete
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAll<string>(
            x => x.Value >= 20,
            page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
#pragma warning restore CS0618
    }

    // --- Obsolete GetAllDescending with LINQ filter ---

    [Fact]
    public async Task GetAllDescending_Obsolete_LinqFilter_WithPaging()
    {
#pragma warning disable CS0618 // Obsolete
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending<string>(
            x => x.Value >= 20,
            page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
#pragma warning restore CS0618
    }

    [Fact]
    public async Task GetAllDescending_Obsolete_LinqFilter_NoPaging()
    {
#pragma warning disable CS0618 // Obsolete
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending<string>(
            x => x.Value >= 20);

        Assert.Equal(2, result.Count);
#pragma warning restore CS0618
    }

    // --- Count ---

    [Fact]
    public async Task Count_WithFilter_ReturnsCount()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var filter = Builders<TestItem>.Filter.Gte(x => x.Value, 20);
        var count = await _readRepo.Count(filter);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Count_NullFilter_ReturnsTotal()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var count = await _readRepo.Count(filterDefinition: null);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Count_JsonFilter_ReturnsCount()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var count = await _readRepo.Count("{ \"Name\": \"Alpha\" }");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Count_LinqExpression_ReturnsCount()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var count = await _readRepo.Count(x => x.Value > 10);

        Assert.Equal(2, count);
    }

    // --- Paging edge cases ---

    [Fact]
    public async Task GetAll_PageSizeLessThan1_ClampsTo1()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));

        var filter = Builders<TestItem>.Filter.Empty;
        var result = await _readRepo.GetAll(filter, page: 1, pageSize: -1);

        // pageSize clamped to 1, so only 1 result
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllDescending_PageLessThan1_ClampsTo1()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAllDescending<string>(
            x => x.Name!,
            page: -1, pageSize: 10);

        // page clamped to 1, returns first page
        Assert.Equal(3, result.Count);
        Assert.Equal("Charlie", result[0].Name);
    }
}
