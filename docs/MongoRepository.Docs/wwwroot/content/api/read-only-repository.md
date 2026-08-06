---
title: IReadOnlyDataRepository
category: API Reference
order: 2
description: Interface for read-only data access operations.
apiRef: "IReadOnlyDataRepository\u00602"
---

## Overview

`IReadOnlyDataRepository<TEntity, TKey>` defines all read operations: `Get`, `GetAll`, `GetAllDescending`, and `Count`. Use this interface when your service only needs to read data.

## Get Methods

Retrieve a single entity by ID, multiple IDs, filter expression, or filter definition:

```csharp
// By ID
var product = await repo.Get("507f1f77bcf86cd799439011");

// By multiple IDs
var products = await repo.Get(new[] { id1, id2, id3 });

// By LINQ expression
var product = await repo.Get(p => p.Name == "Widget");

// By MongoDB FilterDefinition
var filter = Builders<Product>.Filter.Eq(p => p.Name, "Widget");
var product = await repo.Get(filter);
```

### Keys that cannot address a document

If the key cannot be serialized into its stored representation — the common case
being a string that is not a valid 24-digit ObjectId against a key declared as
`[BsonRepresentation(BsonType.ObjectId)]` — `Get(TKey)` returns `null` and
`Get(IEnumerable<TKey>)` returns an empty list. Such a key can never match a
document, so this is treated as "not found" rather than an error. The check runs
while the filter is rendered, before the query is sent.

`Get(IEnumerable<TKey>)` rejects the whole query in that case, including the
well-formed keys in the same call.

### Documents that cannot be deserialized

A document that is found but cannot be mapped onto the entity class — schema
drift, where a persisted field no longer matches the C# property — raises a
`FormatException`. Since 12.0.0 this is **not** reported as `null`: masking it
made broken documents indistinguishable from absent ones. Handle it at the call
site where a read has to stay non-fatal.

## GetAll Methods

Retrieve multiple entities with optional filtering, sorting, and pagination:

```csharp
// All entities
var all = await repo.GetAll();

// With LINQ filter + pagination
var page = await repo.GetAll(
    p => p.Price > 10,
    page: 1,
    pageSize: 20);

// With sorting expression
var sorted = await repo.GetAll(
    p => p.Price > 10,
    p => p.Name,
    page: 1,
    pageSize: 20);

// With MongoDB filter + sort definitions
var filter = Builders<Product>.Filter.Gt(p => p.Price, 10);
var sort = Builders<Product>.Sort.Ascending(p => p.Name);
var result = await repo.GetAll(filter, sort, page: 1, pageSize: 20);

// With JSON filter + sort
var result = await repo.GetAll(
    """{"Price": {"$gt": 10}}""",
    """{"Name": 1}""",
    page: 1,
    pageSize: 20);
```

## GetAllDescending

Same as `GetAll` but with descending sort order:

```csharp
var newest = await repo.GetAllDescending(
    p => p.CreatedAt,
    page: 1,
    pageSize: 10);
```

## Count

Count entities matching a filter:

```csharp
var total = await repo.Count(p => p.IsActive);

var filtered = await repo.Count(
    Builders<Product>.Filter.Gt(p => p.Price, 100));
```
