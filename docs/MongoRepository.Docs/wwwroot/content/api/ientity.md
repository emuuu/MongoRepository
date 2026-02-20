---
title: IEntity
category: API Reference
order: 1
description: Base interface for all MongoDB entities.
apiRef: "IEntity\u00601"
---

## Overview

`IEntity<TKey>` is the base interface that all entities must implement. It defines the required `Id` property used by the repository for CRUD operations.

## Usage

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepository;

public class Product : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

## Custom Key Types

While `string` with `ObjectId` is the most common pattern, you can use other key types:

```csharp
public class Counter : IEntity<int>
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public long Value { get; set; }
}
```

```csharp
public class Event : IEntity<Guid>
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
```
