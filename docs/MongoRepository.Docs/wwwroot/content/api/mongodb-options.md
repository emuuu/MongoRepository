---
title: MongoDbOptions
category: API Reference
order: 7
description: Connection options for MongoDB contexts.
apiRef: MongoDbOptions
---

## Overview

`MongoDbOptions` configures the MongoDB connection strings used by `EntityContext`. It supports separate read-write and read-only connections for replica set deployments.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ReadWriteConnection` | `string` | Connection string for read and write operations |
| `ReadOnlyConnection` | `string` | Connection string for read-only operations (optional) |
| `Serialization` | `MongoSerializationOptions` | Serialization conventions to register with the driver (initialised, everything off by default) |

## Configuration

```json
{
  "MongoDb": {
    "ReadWriteConnection": "mongodb://localhost:27017/MyDatabase",
    "ReadOnlyConnection": "mongodb://secondary:27017/MyDatabase?readPreference=secondaryPreferred"
  }
}
```

```csharp
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));
```

If `ReadOnlyConnection` is not set, read operations fall back to the `ReadWriteConnection`.

## Serialization Conventions

`Serialization` selects the driver conventions the library registers. Every switch is off by default, so a configuration that leaves the section out behaves exactly as before.

| Property | Type | Description |
|----------|------|-------------|
| `IgnoreExtraElements` | `bool?` | `true` reads documents carrying fields the entity class does not declare, `false` registers strict behaviour explicitly, `null` registers nothing |
| `IgnoreIfNull` | `bool?` | `true` leaves null properties out of the stored document, `false` stores them as BSON null, `null` registers nothing |
| `Namespaces` | `IList<string>` | Limits both switches to the listed namespaces; empty covers every type |
| `TypeFilter` | `Func<Type, bool>` | Narrows the scope further, combined with `Namespaces` by AND; can only be set from code |
| `ConventionPackName` | `string` | The name the pack is registered under, `MongoGenericRepository` by default. The driver's own names, `__defaults__` and `__attributes__`, are rejected. Pick a name nothing else registers under — the driver keeps every pack sharing a name and deletes them together |

```json
{
  "MongoDb": {
    "ReadWriteConnection": "mongodb://localhost:27017/MyDatabase",
    "Serialization": {
      "IgnoreExtraElements": true,
      "IgnoreIfNull": false,
      "Namespaces": [ "MyApp.Domain" ]
    }
  }
}
```

Because the two switches are independent, all four cases are available: both on, only one of them, neither — omit the section — or either of them limited to part of the type graph.

```csharp
var mongoDbSection = builder.Configuration.GetSection("MongoDb");
builder.Services.Configure<MongoDbOptions>(mongoDbSection);

MongoRepositoryConventions.Register(mongoDbSection.Get<MongoDbOptions>());
```

`Namespaces` matches whole segments: `MyApp.Orders` covers `MyApp.Orders.Archive`, but not `MyApp.OrdersArchive`. Types without a namespace only match while the list is empty. Entries that are null, empty or whitespace are dropped, and a list that holds nothing usable is rejected with an `ArgumentException` rather than quietly covering every type.
