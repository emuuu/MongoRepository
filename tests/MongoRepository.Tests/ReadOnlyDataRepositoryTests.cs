using MongoDB.Driver;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

// GetAll(x => <predicate>) without type arguments must bind to the non-generic
// predicate overload and filter. Before 12.3.0 it bound to the sorting overload
// with TProperty = bool and returned every document, sorted by the predicate.
//
// GetAllDescending(x => <predicate>) without type arguments binds to a non-generic
// overload marked [Obsolete(error: true)], so the mistake is a compile error
// (CS0619). A compile error cannot be asserted from a test that has to compile;
// GetAllDescending_PredicateOnly_IsObsoleteAsError only checks the attribute.
// Sorting by a boolean stays possible with a named argument, GetAll(sorting: ...),
// which the *_NamedBoolSorting_* tests cover. An explicit type argument alone,
// GetAll<bool>(x => ...), is ambiguous (CS0121) with the obsolete generic filter
// overloads until they are removed in v13; that was already the case before.
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

    public async ValueTask InitializeAsync()
    {
        // Clean up collection before each test
        await _writeRepo.Collection.Database.DropCollectionAsync("TestItems");
    }

    public ValueTask DisposeAsync() => default;

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
    public async Task Get_ById_EmptyKey_ReturnsNull()
    {
        // TestItem stores its key as a plain string, so an empty key is simply a
        // key no document carries. Keys that cannot be serialised at all — and
        // documents that cannot be deserialised — are covered in
        // KeyFormatAndSchemaDriftTests.
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

    // --- GetAll with a predicate ---

    [Fact]
    public async Task GetAll_Predicate_WithoutTypeArguments_Filters()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAll(x => x.Value >= 20 && x.Name != null);

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.True(item.Value >= 20));
    }

    [Fact]
    public async Task GetAll_Predicate_WithPaging_PagesWithinMatches()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        // No sort is applied, so only the page sizes are deterministic, not which match lands on which page.
        var page1 = await _readRepo.GetAll(x => x.Value >= 20, page: 1, pageSize: 1);
        var page2 = await _readRepo.GetAll(x => x.Value >= 20, page: 2, pageSize: 1);
        var page3 = await _readRepo.GetAll(x => x.Value >= 20, page: 3, pageSize: 1);
        var wide = await _readRepo.GetAll(x => x.Value >= 20, page: 1, pageSize: 10);

        Assert.True(Assert.Single(page1).Value >= 20);
        Assert.True(Assert.Single(page2).Value >= 20);
        Assert.Empty(page3);
        Assert.Equal(new[] { "2", "3" }, wide.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task GetAll_Predicate_PageAndPageSizeLessThan1_ClampTo1()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20),
            CreateItem("3", "Charlie", 30));

        var result = await _readRepo.GetAll(x => x.Value >= 20, page: -5, pageSize: 0);

        var item = Assert.Single(result);
        Assert.True(item.Value >= 20);
    }

    [Fact]
    public async Task GetAll_NamedBoolSorting_SortsAndReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20));

        var result = await _readRepo.GetAll(sorting: x => x.Value >= 20);
        var explicitResult = await _readRepo.GetAll<bool>(sorting: x => x.Value >= 20);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.All(result.Skip(1), item => Assert.True(item.Value >= 20));
        // Two documents share the sort key true, so their relative order is not defined.
        Assert.Equal(new[] { false, true, true }, explicitResult.Select(x => x.Value >= 20));
        Assert.Equal(result.Select(x => x.Id).Order(), explicitResult.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task GetAllDescending_NamedBoolSorting_SortsAndReturnsAll()
    {
        await SeedItems(
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Charlie", 30),
            CreateItem("3", "Beta", 20));

        var result = await _readRepo.GetAllDescending(sorting: x => x.Value >= 20);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[2].Name);
        Assert.All(result.Take(2), item => Assert.True(item.Value >= 20));
    }

    [Fact]
    public async Task GetAll_PredicateAndSorting_WithoutTypeArguments_FiltersAndSorts()
    {
        await SeedItems(
            CreateItem("1", "Charlie", 30),
            CreateItem("2", "Alpha", 10),
            CreateItem("3", "Beta", 20));

        var ascending = await _readRepo.GetAll(x => x.Value >= 20, x => x.Name!);
        var descending = await _readRepo.GetAllDescending(x => x.Value >= 20, x => x.Name!);

        Assert.Equal(new[] { "Beta", "Charlie" }, ascending.Select(x => x.Name));
        Assert.Equal(new[] { "Charlie", "Beta" }, descending.Select(x => x.Name));
    }

    [Fact]
    public void GetAllDescending_PredicateOnly_IsObsoleteAsError()
    {
        var signature = new[] { typeof(System.Linq.Expressions.Expression<Func<TestItem, bool>>), typeof(int?), typeof(int?), typeof(IClientSessionHandle), typeof(CancellationToken) };

        foreach (var type in new[] { typeof(IReadOnlyDataRepository<TestItem, string>), typeof(ReadOnlyDataRepository<TestItem, string>) })
        {
            var method = type.GetMethod(nameof(IReadOnlyDataRepository<TestItem, string>.GetAllDescending), signature);

            Assert.NotNull(method);
            Assert.False(method.IsGenericMethodDefinition);
            var obsolete = Assert.Single(method.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Cast<ObsoleteAttribute>());
            Assert.True(obsolete.IsError);
        }
    }

    [Fact]
    public async Task DirectImplementation_WithoutNewMembers_UsesDefaultImplementations()
    {
        await SeedItems(
            CreateItem("3", "Charlie", 30),
            CreateItem("1", "Alpha", 10),
            CreateItem("2", "Beta", 20));
        IReadOnlyDataRepository<TestItem, string> repo = new PreGetAllPredicateRepository(_readRepo);

        var result = await repo.GetAll(x => x.Value >= 20);
        var descending = typeof(IReadOnlyDataRepository<TestItem, string>).GetMethod(
            nameof(IReadOnlyDataRepository<TestItem, string>.GetAllDescending),
            new[] { typeof(System.Linq.Expressions.Expression<Func<TestItem, bool>>), typeof(int?), typeof(int?), typeof(IClientSessionHandle), typeof(CancellationToken) })!;

        Assert.Equal(new[] { "2", "3" }, result.Select(x => x.Id).Order());
        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            descending.Invoke(repo, new object?[] { (System.Linq.Expressions.Expression<Func<TestItem, bool>>)(x => x.Value >= 20), null, null, null, default(CancellationToken) }));
        Assert.IsType<NotSupportedException>(invocation.InnerException);
    }

    // Implements the interface as it was before 12.3.0, without the predicate-only overloads.
#pragma warning disable CS0618 // Obsolete members of the interface are part of that shape
    private sealed class PreGetAllPredicateRepository(IReadOnlyDataRepository<TestItem, string> inner) : IReadOnlyDataRepository<TestItem, string>
    {
        public Task<TestItem> Get(string id, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Get(id, session, cancellationToken);
        public Task<List<TestItem>> Get(IEnumerable<string> ids, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Get(ids, session, cancellationToken);
        public Task<TestItem> Get(FilterDefinition<TestItem> filterDefinition = null!, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Get(filterDefinition, session, cancellationToken);
        public Task<TestItem> Get<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, CancellationToken cancellationToken = default) => inner.Get<TProperty>(filter, cancellationToken);
        public Task<List<TestItem>> GetAll(IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAll(session, cancellationToken);
        public Task<List<TestItem>> GetAll(FilterDefinition<TestItem> filterDefinition, SortDefinition<TestItem> sortDefinition = null!, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAll(filterDefinition, sortDefinition, page, pageSize, session, cancellationToken);
        public Task<List<TestItem>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition = null!, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAll(jsonFilterDefinition, jsonSortingDefinition, page, pageSize, session, cancellationToken);
        public Task<List<TestItem>> GetAll<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default) => inner.GetAll<TProperty>(filter, page, pageSize, cancellationToken);
        public Task<List<TestItem>> GetAll<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAll(sorting: sorting, page, pageSize, session, cancellationToken);
        public Task<List<TestItem>> GetAll<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, System.Linq.Expressions.Expression<Func<TestItem, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAll(filter, sorting, page, pageSize, session, cancellationToken);
        public Task<List<TestItem>> GetAllDescending<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default) => inner.GetAllDescending<TProperty>(filter, page, pageSize, cancellationToken);
        public Task<List<TestItem>> GetAllDescending<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAllDescending(sorting: sorting, page, pageSize, session, cancellationToken);
        public Task<List<TestItem>> GetAllDescending<TProperty>(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, System.Linq.Expressions.Expression<Func<TestItem, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.GetAllDescending(filter, sorting, page, pageSize, session, cancellationToken);
        public Task<long> Count(FilterDefinition<TestItem> filterDefinition = null!, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Count(filterDefinition, session, cancellationToken);
        public Task<long> Count(string jsonFilterDefinition, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Count(jsonFilterDefinition, session, cancellationToken);
        public Task<long> Count(System.Linq.Expressions.Expression<Func<TestItem, bool>> filter, IClientSessionHandle session = null!, CancellationToken cancellationToken = default) => inner.Count(filter, session, cancellationToken);
    }
#pragma warning restore CS0618

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
