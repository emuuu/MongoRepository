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
