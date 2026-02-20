---
title: Attributes
category: Getting Started
order: 2
description: Use attributes to customize collection and database names for your entities.
---

## EntityCollectionAttribute

By default, MongoRepository uses the entity class name as the MongoDB collection name. Use `[EntityCollection]` to override:

```csharp
[EntityCollection("products_v2")]
public class Product : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = "";
}
```

## EntityDatabaseAttribute

Use `[EntityDatabase]` to store an entity in a different database than the default:

```csharp
[EntityDatabase("ArchiveDb")]
[EntityCollection("archived_orders")]
public class ArchivedOrder : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public DateTime ArchivedAt { get; set; }
}
```

## Combining Attributes

Both attributes can be used together to fully control where entities are stored:

```csharp
[EntityDatabase("ReportingDb")]
[EntityCollection("daily_reports")]
public class DailyReport : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
}
```
