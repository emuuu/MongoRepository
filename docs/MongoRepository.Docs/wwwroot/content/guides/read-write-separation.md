---
title: Read/Write Separation
category: Guides
order: 2
description: Use separate connections for read and write operations in replica sets.
---

## Overview

MongoRepository supports separate read-only and read-write connection strings. This is useful for MongoDB replica set deployments where you want to direct read traffic to secondary nodes.

## Configuration

```json
{
  "MongoDb": {
    "ReadWriteConnection": "mongodb://primary:27017/MyDatabase",
    "ReadOnlyConnection": "mongodb://secondary:27017/MyDatabase?readPreference=secondaryPreferred"
  }
}
```

## How It Works

- `ReadWriteRepository` uses the **read-write** connection for all operations
- `ReadOnlyDataRepository` uses the **read-only** connection for all read operations
- If no read-only connection is configured, all operations use the read-write connection

## Read-Only Repository

For services that only need to read data, use `ReadOnlyDataRepository` directly:

```csharp
public interface IProductReader : IReadOnlyDataRepository<Product, string> { }

public class ProductReader
    : ReadOnlyDataRepository<Product, string>, IProductReader
{
    public ProductReader(EntityContext context) : base(context) { }
}
```

This ensures read traffic goes to the secondary node while write operations are always routed to the primary.
