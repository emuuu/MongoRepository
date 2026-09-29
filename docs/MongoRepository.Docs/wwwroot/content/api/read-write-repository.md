---
title: IReadWriteRepository
category: API Reference
order: 3
description: Interface for full CRUD operations (read + write).
apiRef: "IReadWriteRepository\u00602"
---

## Overview

`IReadWriteRepository<TEntity, TKey>` extends `IReadOnlyDataRepository` with write operations: `Add`, `AddRange`, `Update`, and `Delete`. This is the interface you'll use most often.

## Add

Insert a single entity:

```csharp
var product = new Product { Name = "Widget", Price = 9.99m };
await repo.Add(product);
// product.Id is now set
```

## AddRange

Insert multiple entities in a single batch operation:

```csharp
var products = new[]
{
    new Product { Name = "Widget", Price = 9.99m },
    new Product { Name = "Gadget", Price = 19.99m },
    new Product { Name = "Doohickey", Price = 4.99m }
};
await repo.AddRange(products);
```

## Update

Replace a single entity or bulk-update multiple entities:

```csharp
// Single update
product.Price = 12.99m;
var result = await repo.Update(product);

// Bulk update
var updatedProducts = products.Select(p => { p.Price *= 1.1m; return p; });
var bulkResult = await repo.Update(updatedProducts);
```

## String Trimming

`Add`, `AddRange` and both `Update` overloads trim leading and trailing whitespace from the entity's string properties before writing. What is and is not trimmed:

- **Trimmed:** every readable and writable `string` property of the entity itself.
- **Not trimmed:** the key (`Id`, and whichever member is stored as `_id`), properties marked `[BsonIgnore]` or `[NoTrim]`, null, empty and whitespace-only values, and strings inside nested objects or collections.

The key is stored exactly as passed, so the same value finds the document again through `Get`, `Update` and `Delete` — none of them trims either. A key with surrounding whitespace is a different key than its trimmed form; normalise it yourself before building it if that is what you want.

The trim happens **in place**: the instance you pass in carries the trimmed values afterwards, also when the write fails or a transaction is rolled back. `AddRange` and the bulk `Update` enumerate the sequence once, so a lazy projection is neither run twice nor replaced by untrimmed copies.

## Delete

Delete by ID, multiple IDs, filter expression, or filter definition:

```csharp
// By ID
await repo.Delete("507f1f77bcf86cd799439011");

// By multiple IDs
await repo.Delete(new[] { id1, id2, id3 });

// By LINQ expression
await repo.Delete(p => p.Price < 1.00m);

// By MongoDB FilterDefinition
var filter = Builders<Product>.Filter.Lt(p => p.Price, 1.00m);
await repo.Delete(filter);
```
