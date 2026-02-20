using MongoDB.Bson.Serialization.Attributes;

namespace MongoRepository.Tests.Infrastructure;

[EntityDatabase("TestDb")]
[EntityCollection("TestItems")]
public class TestItem : IEntity<string>
{
    [BsonId]
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public int Value { get; set; }

    public string? Description { get; set; }

    [BsonIgnore]
    public string? IgnoredProperty { get; set; }
}

public class PlainEntity : IEntity<string>
{
    [BsonId]
    public string Id { get; set; } = null!;

    public string? Name { get; set; }
}
