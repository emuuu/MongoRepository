namespace MongoRepository.Tests;

public class EntityCollectionAttributeTests
{
    [Fact]
    public void DefaultConstructor_CollectionIsNull()
    {
        var attr = new EntityCollectionAttribute();

        Assert.Null(attr.Collection);
    }

    [Fact]
    public void Constructor_SetsCollection()
    {
        var attr = new EntityCollectionAttribute("MyCollection");

        Assert.Equal("MyCollection", attr.Collection);
    }

    [Fact]
    public void Property_CanBeSet()
    {
        var attr = new EntityCollectionAttribute();
        attr.Collection = "Updated";

        Assert.Equal("Updated", attr.Collection);
    }
}
