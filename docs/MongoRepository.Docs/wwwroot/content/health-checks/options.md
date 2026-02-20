---
title: Health Check Options
category: Health Checks
order: 2
description: Configure how the MongoDB health check evaluates connection failures.
apiRef: MongoRepositoryHealthCheckOptions
---

## Overview

`MongoRepositoryHealthCheckOptions` controls how the health check evaluates failures and missing connections.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SingleFailureIsUnhealthy` | `bool` | `false` | If true, a single failing connection results in Unhealthy instead of Degraded |
| `MissingConnectionIsFailure` | `bool` | `false` | If true, a missing (empty) connection string counts as a failure |

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddMongoRepository(options =>
    {
        // Strict mode: any single failure = Unhealthy
        options.SingleFailureIsUnhealthy = true;

        // Require both connections to be configured
        options.MissingConnectionIsFailure = true;
    });
```

## Extension Methods

The `AddMongoRepository` extension supports additional ASP.NET Core health check parameters:

```csharp
builder.Services.AddHealthChecks()
    .AddMongoRepository(
        configure: options => { options.SingleFailureIsUnhealthy = true; },
        name: "mongodb",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "mongodb" },
        timeout: TimeSpan.FromSeconds(5));
```
