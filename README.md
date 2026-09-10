<p align="center">
  <img src="mongorepository_logo.svg" alt="MongoRepository" width="128" />
</p>

# MongoRepository

Generic, extensible CRUD repository for MongoDB — targeting .NET 8, 9, and 10.

[![NuGet MongoGenericRepository](https://img.shields.io/nuget/v/MongoGenericRepository.svg?label=MongoGenericRepository)](https://www.nuget.org/packages/MongoGenericRepository)
[![NuGet HealthChecks](https://img.shields.io/nuget/v/MongoGenericRepository.HealthChecks.svg?label=HealthChecks)](https://www.nuget.org/packages/MongoGenericRepository.HealthChecks)
[![CI](https://github.com/emuuu/MongoRepository/actions/workflows/ci.yml/badge.svg)](https://github.com/emuuu/MongoRepository/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docs](https://img.shields.io/badge/Docs-GitHub%20Pages-blue)](https://emuuu.github.io/MongoRepository/)

| Package | Description |
|---|---|
| **MongoGenericRepository** | Generic read/write repository with read/write separation support |
| **MongoGenericRepository.HealthChecks** | ASP.NET Core health checks for MongoDB connections |

## Installation

```bash
dotnet add package MongoGenericRepository

# Optional: health checks
dotnet add package MongoGenericRepository.HealthChecks
```

## Quick Start

Configure your connection strings:

```json
{
  "MongoDbOptions": {
    "ReadWriteConnection": "mongodb://localhost:27017/MyDatabase",
    "ReadOnlyConnection": "mongodb://secondary:27017/MyDatabase?readPreference=secondaryPreferred"
  }
}
```

Define an entity:

```csharp
[EntityDatabase("MyDatabase")]
[EntityCollection("Products")]
public class Product : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}
```

Create a repository:

```csharp
public interface IProductRepository : IReadWriteRepository<Product, string>
{
    Task<List<Product>> GetByCategory(string category);
}

public class ProductRepository : ReadWriteRepository<Product, string>, IProductRepository
{
    public ProductRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions) { }

    public async Task<List<Product>> GetByCategory(string category)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Category, category);
        return await Collection.Find(filter).ToListAsync();
    }
}
```

Register services:

```csharp
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDbOptions"));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

## Serialization Conventions

The driver is strict when it deserializes: a document that carries a field the entity class does not declare fails with a `FormatException`. That is the normal state of affairs during a rolling update, where instances running the newer schema already write fields the older ones have never heard of. The driver answers this with process-wide conventions — MongoRepository lets you configure them instead of putting `[BsonIgnoreExtraElements]` on every entity class and `[BsonIgnoreIfNull]` on every nullable property.

Nothing is registered unless you ask for it, so leaving the section out keeps the driver's own behaviour.

```json
{
  "MongoDbOptions": {
    "ReadWriteConnection": "mongodb://localhost:27017/MyDatabase",
    "Serialization": {
      "IgnoreExtraElements": true,
      "IgnoreIfNull": false,
      "Namespaces": [ "MyApp.Domain", "MyApp.Reporting.Entities" ]
    }
  }
}
```

| Setting | Effect |
|---|---|
| `IgnoreExtraElements` | `true` reads documents carrying unknown fields, `false` registers strict behaviour explicitly, omitted registers nothing |
| `IgnoreIfNull` | `true` leaves null properties out of the stored document, `false` stores them as BSON null, omitted registers nothing |
| `Namespaces` | Limits both switches to the listed namespaces; empty or omitted covers every type. A list that holds nothing usable is rejected rather than quietly covering every type |
| `ConventionPackName` | The name the pack is registered under, `MongoGenericRepository` by default. The driver's own names, `__defaults__` and `__attributes__`, are rejected. Pick a name nothing else registers under — the driver keeps every pack sharing a name and deletes them together |

The two switches are independent, which covers all four cases: both on, only one of them, neither — omit the section, and the library behaves exactly as it did before — or either of them limited to part of the type graph via `Namespaces`.

Register them before the first repository call:

```csharp
var mongoDbSection = builder.Configuration.GetSection("MongoDbOptions");
builder.Services.Configure<MongoDbOptions>(mongoDbSection);

MongoRepositoryConventions.Register(mongoDbSection.Get<MongoDbOptions>());
```

`TypeFilter` narrows the scope further. It can only be set from code — the configuration binder skips delegate properties — and is combined with `Namespaces` by AND:

```csharp
MongoRepositoryConventions.Register(new MongoSerializationOptions
{
    IgnoreExtraElements = true,
    Namespaces = { "MyApp.Domain" },
    TypeFilter = type => !type.Name.EndsWith("Snapshot")
});
```

### What to expect

- **Only class maps built after the call are affected.** The driver maps an entity class once per process, on first use, and never revisits it. A repository call that runs before the registration leaves that entity strict for the rest of the process — which is why the call belongs at the start of start-up.
- **Attributes win over conventions.** `[BsonIgnoreExtraElements(false)]` keeps an entity strict while a tolerant convention is registered, and `[BsonIgnoreExtraElements]` keeps it tolerant against a strict one. The driver applies attribute conventions last.
- **`IgnoreIfNull` changes what is stored, not only how it is read.** A null property is absent from the document instead of being stored as null, which `$exists` filters and sparse or partial indexes react to. Documents written before the switch was turned on keep their nulls.
- **Registration happens once, process-wide.** An application that never calls `Register` gets them from the first `EntityContext` constructed with options that ask for a convention — a context whose switches are all unset leaves the state open for a later one. Later `MongoDbOptions` instances carrying different values do not change an existing registration. `MongoRepositoryConventions.Unregister()` removes the pack again, but class maps that already exist keep the behaviour they were built with.
- **Namespaces match whole segments.** `MyApp.Orders` covers `MyApp.Orders.Archive`, but not `MyApp.OrdersArchive`.

## Read/Write Separation

MongoRepository supports separate connections for read and write operations — useful for directing read traffic to secondary nodes in replica sets. If `ReadOnlyConnection` is not set, all operations fall back to `ReadWriteConnection`.

```csharp
// Read-only repository for services that only need to query data
public class ProductReader : ReadOnlyDataRepository<Product, string>, IProductReader
{
    public ProductReader(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions) { }
}
```

## Repository API

| Method | Description |
|---|---|
| `Get(id)` | Get entity by ID |
| `Get(ids)` | Get multiple entities by IDs |
| `Get(filter)` | Get single entity by filter |
| `GetAll()` | Get all entities (with optional filter, sort, pagination) |
| `Count(filter)` | Count matching entities |
| `Add(entity)` | Insert a single entity |
| `AddRange(entities)` | Insert multiple entities |
| `Update(entity)` | Replace a single entity |
| `Update(entities)` | Bulk replace multiple entities |
| `Delete(id)` | Delete by ID |
| `Delete(ids)` | Delete multiple by IDs |
| `Delete(filter)` | Delete by filter |

All methods support `CancellationToken` and accept native MongoDB driver types (`FilterDefinition<T>`, `SortDefinition<T>`, etc.).

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddMongoRepository(options =>
    {
        options.SingleFailureIsUnhealthy = true;
        options.MissingConnectionIsFailure = false;
    });

app.MapHealthChecks("/health");
```

| Scenario | Default | SingleFailureIsUnhealthy |
|---|---|---|
| Both connections OK | Healthy | Healthy |
| One connection fails | Degraded | Unhealthy |
| Both connections fail | Unhealthy | Unhealthy |

## Documentation

Full documentation with API reference: **[emuuu.github.io/MongoRepository](https://emuuu.github.io/MongoRepository/)**

## License

MIT
