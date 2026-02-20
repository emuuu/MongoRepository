---
title: EntityDatabaseAttribute
category: API Reference
order: 6
description: Attribute to specify a custom MongoDB database name for an entity.
apiRef: EntityDatabaseAttribute
---

## Overview

`[EntityDatabase]` overrides the default database (from connection string) with a custom database name for a specific entity.

## Usage

```csharp
[EntityDatabase("ArchiveDb")]
public class ArchivedOrder : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public DateTime ArchivedAt { get; set; }
}
```

This stores `ArchivedOrder` entities in the `ArchiveDb` database instead of the default database.
