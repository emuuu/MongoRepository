namespace MongoRepository.Tests;

public class EntityDatabaseAttributeTests
{
    [Fact]
    public void DefaultConstructor_DatabaseIsNull()
    {
        var attr = new EntityDatabaseAttribute();

        Assert.Null(attr.Database);
    }

    [Fact]
    public void Constructor_SetsDatabase()
    {
        var attr = new EntityDatabaseAttribute("MyDatabase");

        Assert.Equal("MyDatabase", attr.Database);
    }

    [Fact]
    public void Property_CanBeSet()
    {
        var attr = new EntityDatabaseAttribute();
        attr.Database = "Updated";

        Assert.Equal("Updated", attr.Database);
    }
}
