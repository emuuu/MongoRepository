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

## Serialization Conventions

The driver is strict when it deserializes: a document that carries a field the entity class does not declare fails with a `FormatException`. During a rolling update that is business as usual, because instances running the newer schema already write fields the older ones do not know. `MongoDbOptions.Serialization` configures the driver conventions that relax this, instead of putting `[BsonIgnoreExtraElements]` on every entity class and `[BsonIgnoreIfNull]` on every nullable property.

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

Both switches are independent and off by default, which covers all four cases: both on, only one of them, neither — omit the section and nothing is registered — or either of them limited to the namespaces you list. `TypeFilter` narrows the scope further and can only be set from code, as the configuration binder skips delegate properties; it is combined with `Namespaces` by AND.

Register the conventions before the first repository call:

```csharp
var mongoDbSection = builder.Configuration.GetSection("MongoDb");
builder.Services.Configure<MongoDbOptions>(mongoDbSection);

MongoRepositoryConventions.Register(mongoDbSection.Get<MongoDbOptions>());
```

What to expect:

- Only class maps built after the call are affected. The driver maps an entity class once per process, on first use, and never revisits it — a repository call that runs earlier leaves that entity strict for the rest of the process.
- Attributes win over conventions. `[BsonIgnoreExtraElements(false)]` keeps an entity strict while a tolerant convention is registered, and `[BsonIgnoreExtraElements]` keeps it tolerant against a strict one.
- `IgnoreIfNull` changes what is stored, not only how it is read: a null property is absent from the document instead of stored as null, which `$exists` filters and sparse or partial indexes react to.
- Registration happens once, process-wide. An application that never calls `Register` gets them from the first `EntityContext` constructed with options that ask for a convention; a context whose switches are all unset leaves the state open for a later one, and once something is registered, later `MongoDbOptions` instances carrying different values do not change it.
- Namespaces match whole segments: `MyApp.Orders` covers `MyApp.Orders.Archive`, but not `MyApp.OrdersArchive`.

## EntityContext

The `EntityContext` manages MongoDB client instances and provides access to collections. It caches `MongoClient` instances per connection string to avoid connection pool waste.

```csharp
// EntityContext is typically injected via DI
builder.Services.AddSingleton<EntityContext>();
```
