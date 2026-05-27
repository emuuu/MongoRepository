# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## MongoGenericRepository / MongoGenericRepository.HealthChecks

### [Unreleased]

#### Added

- Documentation site with API reference (Blazor WebAssembly + GitHub Pages)
- GitHub Actions CI/CD workflows
- Health check add-on package (`MongoGenericRepository.HealthChecks`)

### [11.0.0]

#### Added

- `StartSessionAsync(CancellationToken)` on `IReadWriteRepository` — returns an `IClientSessionHandle` bound to the read/write client backing the repository's collection. Dispose with `using var session = ...` (the driver's handle is `IDisposable`, not `IAsyncDisposable`).
- `SupportsTransactionsAsync(CancellationToken)` on `IReadWriteRepository` — best-effort capability check. Returns `true` for `ReplicaSet` / `Sharded` / `LoadBalanced` topologies, including direct connections to a replica set member (`directConnection=true`). Returns `false` for standalone deployments and for any failure during the probe (so the result is safe to gate logic on without an extra try/catch). Performs a `ping` per call; not cached.
- `ExecuteInTransactionAsync(Func<IClientSessionHandle, CancellationToken, Task>, CancellationToken)` on `IReadWriteRepository` — wraps the driver's `WithTransactionAsync`, committing on success and aborting on exception, with automatic retries on `TransientTransactionError` and `UnknownTransactionCommitResult`. **The work delegate may run more than once: keep it idempotent, do not trigger non-repeatable side effects, do not swallow exceptions, and pass the supplied session to every repository call inside the delegate** (operations without `session:` run outside the transaction and break atomicity).
- Optional `IClientSessionHandle session = null` parameter on all non-obsolete mutation methods: `Add`, `AddRange`, `Update` (single and bulk), `Delete` (by id, by ids, by filter, by expression).
- Regression tests confirming `MongoWriteException` with `ServerErrorCategory.DuplicateKey` is surfaced for sparse unique compound index violations, both on the sessionless and the session-bound `Add` path. The first document persists; subsequent inserts with the same compound key are rejected.

#### Changed

- **Breaking — signature change.** All non-obsolete read and mutation methods on `IReadOnlyDataRepository<TEntity, TKey>` and `IReadWriteRepository<TEntity, TKey>` now take an optional `IClientSessionHandle session = null` parameter, inserted **before** `CancellationToken cancellationToken = default`. Affected methods:
  - Reads: `Get(TKey, ...)`, `Get(IEnumerable<TKey>, ...)`, `Get(FilterDefinition<TEntity>, ...)`, `GetAll(...)`, `GetAll(FilterDefinition, SortDefinition, page, pageSize, ...)`, `GetAll(string jsonFilter, string jsonSort, page, pageSize, ...)`, `Count(FilterDefinition, ...)`, `Count(string jsonFilter, ...)`.
  - Writes: `Add`, `AddRange`, both `Update` overloads, all four `Delete` overloads.
  - The `[Obsolete]` LINQ-expression overloads were left untouched and continue to be removed in v11. They cannot participate in a transaction; migrate to the non-obsolete equivalents if you need transactional behaviour.

#### Migration

- **Named-argument callers are unaffected.** `await repo.Get(id, cancellationToken: ct)` continues to compile.
- **Positional callers that passed the `CancellationToken` as the second argument must switch to named:** `await repo.Get(id, ct)` → `await repo.Get(id, cancellationToken: ct)`. Same for `Delete(id, ct)`, `Add(entity, options, ct)`, etc.
- **External implementers / mocks of the repository interfaces must add the new parameter** to every overridden method signature; the compiler will surface the missing-member errors.
- **Reads inside a transaction bypass the read-only collection.** When a session is passed to `Get`/`GetAll`/`Count`, the call routes to the read/write collection because the session is client-bound. If your deployment uses separate read-only and read/write connection strings, transactional reads will not be served by read replicas — this is required for correctness and intentional.

### [10.2.0]

#### Changed

- Upgraded to MongoDB.Driver 3.6.0
- Multi-target support for net8.0, net9.0, net10.0, and netstandard2.1

#### Added

- `EntityContext` with static `MongoClient` connection caching
- Read/write connection separation for replica set deployments
- `EntityDatabaseAttribute` and `EntityCollectionAttribute` for custom naming
- Pagination support (`page`, `pageSize`) on `GetAll` methods
- `CancellationToken` support on all repository methods
- `Count` overloads with `FilterDefinition`, JSON filter, and expression filter
- Bulk `Update` and `Delete` operations

For older versions, see [GitHub Releases](https://github.com/emuuu/MongoRepository/releases).
