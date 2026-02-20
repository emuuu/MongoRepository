---
title: Installation
category: Getting Started
order: 1
description: Install and configure MongoRepository in your .NET project.
---

## NuGet Package

Install the main package via NuGet:

```bash
dotnet add package MongoGenericRepository
```

For health checks support:

```bash
dotnet add package MongoGenericRepository.HealthChecks
```

## Configuration

Add your MongoDB connection settings to `appsettings.json`:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "MyDatabase"
  }
}
```

## Register Services

In your `Program.cs`, register the MongoDB context:

```csharp
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<EntityContext>();
```

Then register your repositories:

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

## Define an Entity

Create an entity class implementing `IEntity<string>`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepository;

public class Product : IEntity<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

## Create a Repository

```csharp
using MongoRepository;

public interface IProductRepository : IReadWriteRepository<Product> { }

public class ProductRepository : ReadWriteRepository<Product>, IProductRepository
{
    public ProductRepository(EntityContext context) : base(context) { }
}
```

## Next Steps

- Learn about [custom attributes](/docs/getting-started/attributes) for collection and database naming
- Browse the [API Reference](/docs/api/read-only-repository) for all available methods
- Set up [Health Checks](/docs/health-checks/setup) for production monitoring
