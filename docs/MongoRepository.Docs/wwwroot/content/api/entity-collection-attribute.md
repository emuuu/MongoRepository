---
title: EntityCollectionAttribute
category: API Reference
order: 5
description: Attribute to specify a custom MongoDB collection name for an entity.
apiRef: EntityCollectionAttribute
---

## Overview

`[EntityCollection]` overrides the default collection name (entity class name) with a custom name.

## Usage

```csharp
[EntityCollection("product_catalog")]
public class Product : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = "";
}
```

This stores `Product` entities in the `product_catalog` collection instead of `Product`.
