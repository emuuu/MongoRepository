using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests;

[Collection("MongoDB")]
public class EntityContextTests
{
    private readonly MongoDbFixture _fixture;

    public EntityContextTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_WithAttributes_UsesAttributeValues()
    {
        var options = _fixture.CreateOptions();

        var context = new EntityContext<TestItem>(options);

        var collection = context.Collection(true);
        Assert.Equal("TestItems", collection.CollectionNamespace.CollectionName);
        Assert.Equal("TestDb", collection.Database.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void Constructor_WithoutAttributes_FallsBackToTypeName()
    {
        var options = _fixture.CreateOptions();

        var context = new EntityContext<PlainEntity>(options);

        var collection = context.Collection(true);
        Assert.Equal("PlainEntity", collection.CollectionNamespace.CollectionName);
        Assert.Equal("PlainEntity", collection.Database.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void Collection_ReadOnly_ReturnsCollection()
    {
        var options = _fixture.CreateOptions();
        var context = new EntityContext<TestItem>(options);

        var collection = context.Collection(readOnly: true);

        Assert.NotNull(collection);
    }

    [Fact]
    public void Collection_ReadWrite_ReturnsCollection()
    {
        var options = _fixture.CreateOptions();
        var context = new EntityContext<TestItem>(options);

        var collection = context.Collection(readOnly: false);

        Assert.NotNull(collection);
    }
}
