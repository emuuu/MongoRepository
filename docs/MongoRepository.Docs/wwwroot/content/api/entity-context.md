---
title: EntityContext
category: API Reference
order: 4
description: MongoDB context that manages client connections and collection access.
apiRef: "EntityContext\u00601"
---

## Overview

`EntityContext` is the central class that manages MongoDB client instances and provides typed access to collections. It reads connection strings from `MongoDbOptions` and caches `MongoClient` instances per connection string.

## Registration

```csharp
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<EntityContext>();
```

## How It Works

When a repository accesses its collection, `EntityContext` determines:

1. Which **database** to use (from `[EntityDatabase]` attribute or the default from the connection string)
2. Which **collection** to use (from `[EntityCollection]` attribute or the entity class name)
3. Whether to use the **read-only** or **read-write** connection

## Custom Repository Usage

In custom repositories, you can access the underlying `IMongoCollection<T>` for advanced queries:

```csharp
public class ProductRepository : ReadWriteRepository<Product, string>
{
    public ProductRepository(EntityContext context) : base(context) { }

    public async Task<List<Product>> GetExpensive(decimal minPrice)
    {
        var filter = Builders<Product>.Filter.Gte(p => p.Price, minPrice);
        var sort = Builders<Product>.Sort.Descending(p => p.Price);

        return await Collection
            .Find(filter)
            .Sort(sort)
            .Limit(100)
            .ToListAsync();
    }
}
```
