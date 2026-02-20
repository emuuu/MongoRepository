---
title: Configuration
category: Getting Started
order: 3
description: Configure MongoDB connection strings and options for your application.
apiRef: MongoDbOptions
---

## MongoDbOptions

MongoRepository uses the `MongoDbOptions` class to configure database connections. It supports separate read-write and read-only connection strings for replica set scenarios.

## Basic Setup

```json
{
  "MongoDb": {
    "ReadWriteConnection": "mongodb://localhost:27017/MyDatabase"
  }
}
```

```csharp
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<EntityContext>();
```

## Read/Write Separation

For replica set deployments, you can configure separate connections:

```json
{
  "MongoDb": {
    "ReadWriteConnection": "mongodb://primary:27017/MyDatabase",
    "ReadOnlyConnection": "mongodb://secondary:27017/MyDatabase?readPreference=secondaryPreferred"
  }
}
```

The `ReadOnlyDataRepository` will use the read-only connection when available, falling back to the read-write connection.

## EntityContext

The `EntityContext` manages MongoDB client instances and provides access to collections. It caches `MongoClient` instances per connection string to avoid connection pool waste.

```csharp
// EntityContext is typically injected via DI
builder.Services.AddSingleton<EntityContext>();
```
