using MongoDB.Bson;
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

[EntityDatabase("TestDb")]
[EntityCollection("OriginMarkerItems")]
public class OriginMarkerItem : IEntity<string>
{
    [BsonId]
    public string Id { get; set; } = null!;

    public string? OriginEventId { get; set; }

    public string? OriginDiscriminator { get; set; }
}

/// <summary>
/// Entity whose string key is stored as an ObjectId. Serialising a key that is
/// not a valid 24-digit hex string fails while the filter is rendered — before
/// the query reaches the server.
/// </summary>
[EntityDatabase("TestDb")]
[EntityCollection("ObjectIdKeyedItems")]
public class ObjectIdKeyedItem : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string? Name { get; set; }
}

/// <summary>
/// Entity used to reproduce schema drift: the stored document carries a
/// <c>Value</c> that no longer matches the <see cref="int"/> declared here,
/// so deserialisation fails once the document has been fetched.
/// </summary>
[EntityDatabase("TestDb")]
[EntityCollection("DriftItems")]
public class DriftItem : IEntity<string>
{
    [BsonId]
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public int Value { get; set; }
}

/// <summary>
/// Entity whose stored key is not the <see cref="IEntity{TKey}.Id"/> property but
/// a differently named member carrying <c>[BsonId]</c>.
/// </summary>
[EntityDatabase("TestDb")]
[EntityCollection("AlternateKeyItems")]
public class AlternateKeyItem : IEntity<string>
{
    [BsonId]
    public string Key { get; set; } = null!;

    public string Id { get; set; } = null!;

    public string? Name { get; set; }
}

[EntityDatabase("TestDb")]
[EntityCollection("NoTrimItems")]
public class NoTrimItem : IEntity<string>
{
    [BsonId]
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    [NoTrim]
    public string? Signature { get; set; }
}
