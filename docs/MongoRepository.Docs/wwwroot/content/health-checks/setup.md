---
title: Health Check Setup
category: Health Checks
order: 1
description: Monitor MongoDB connectivity with ASP.NET Core health checks.
apiRef: MongoRepositoryHealthCheck
---

## Installation

```bash
dotnet add package MongoGenericRepository.HealthChecks
```

## Basic Registration

```csharp
builder.Services.AddHealthChecks()
    .AddMongoRepository();
```

This registers a health check named `"MongoRepository"` that tests both the read-write and read-only connections.

## Custom Options

```csharp
builder.Services.AddHealthChecks()
    .AddMongoRepository(options =>
    {
        options.SingleFailureIsUnhealthy = true;
        options.MissingConnectionIsFailure = false;
    });
```

## Map Health Endpoint

```csharp
app.MapHealthChecks("/health");
```

## Health Status Logic

| Scenario | Default | SingleFailureIsUnhealthy |
|----------|---------|--------------------------|
| Both connections OK | Healthy | Healthy |
| One connection fails | Degraded | Unhealthy |
| Both connections fail | Unhealthy | Unhealthy |
| Connection string empty | Skipped | Skipped (or Failure if `MissingConnectionIsFailure`) |
