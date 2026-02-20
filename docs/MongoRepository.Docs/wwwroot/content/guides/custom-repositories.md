---
title: Custom Repositories
category: Guides
order: 1
description: Extend the base repository with custom query methods.
---

## Creating a Custom Repository

Inherit from `ReadWriteRepository` and add domain-specific methods:

```csharp
public interface IProductRepository : IReadWriteRepository<Product, string>
{
    Task<List<Product>> GetByCategory(string category);
    Task<Product?> GetMostExpensive();
}

public class ProductRepository
    : ReadWriteRepository<Product, string>, IProductRepository
{
    public ProductRepository(EntityContext context) : base(context) { }

    public async Task<List<Product>> GetByCategory(string category)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Category, category);
        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<Product?> GetMostExpensive()
    {
        return await Collection
            .Find(FilterDefinition<Product>.Empty)
            .SortByDescending(p => p.Price)
            .Limit(1)
            .FirstOrDefaultAsync();
    }
}
```

## Accessing the Collection

The `Collection` property gives you direct access to the underlying `IMongoCollection<T>` for any MongoDB Driver operation:

```csharp
public async Task<long> CountExpensive(decimal threshold)
{
    return await Collection.CountDocumentsAsync(
        Builders<Product>.Filter.Gte(p => p.Price, threshold));
}
```

## Dependency Injection

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```
