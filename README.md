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
