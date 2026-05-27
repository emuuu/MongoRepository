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
- `SupportsTransactionsAsync(CancellationToken)` on `IReadWriteRepository` — best-effort capability check. Returns `true` for `ReplicaSet` / `Sharded` / `LoadBalanced` topologies, including direct connections to a replica set member (`directConnection=true`). Returns `false` for standalone deployments and for any failure during the probe (so the result is safe to gate logic on without an extra try/catch). Performs a `ping` per call; not cached, because cluster topology can change at runtime (failover, reconfig).
- `ExecuteInTransactionAsync(Func<IClientSessionHandle, CancellationToken, Task>, CancellationToken)` on `IReadWriteRepository` — wraps the driver's `WithTransactionAsync`, committing on success and aborting on exception, with automatic retries on `TransientTransactionError` and `UnknownTransactionCommitResult`. **The work delegate may run more than once: keep it idempotent, do not trigger non-repeatable side effects, do not swallow exceptions, and pass the supplied session to every repository call inside the delegate** (operations without `session:` run outside the transaction and break atomicity).
- Optional `IClientSessionHandle session = null` parameter on all non-obsolete mutation methods (`Add`, `AddRange`, both `Update` overloads, all four `Delete` overloads) and on all non-obsolete read methods (`Get(TKey)`, `Get(IEnumerable<TKey>)`, `Get(FilterDefinition)`, `GetAll()`, `GetAll(FilterDefinition, SortDefinition, page, pageSize)`, `GetAll(string jsonFilter, string jsonSort, page, pageSize)`, `Count(FilterDefinition)`, `Count(string jsonFilter)`, plus the LINQ-expression overloads `GetAll<TProperty>(sort)`, `GetAll<TProperty>(filter, sort)`, `GetAllDescending<TProperty>(sort)`, `GetAllDescending<TProperty>(filter, sort)`, `Count(Expression<Func<TEntity, bool>>)`).
- Regression tests confirming `MongoWriteException` with `ServerErrorCategory.DuplicateKey` is surfaced for sparse unique compound index violations, both on the sessionless and the session-bound `Add` path, including inside an open transaction. The first document persists; subsequent inserts with the same compound key are rejected.

#### Changed

- **Breaking — signature change on every non-obsolete read and mutation method.** `IReadOnlyDataRepository<TEntity, TKey>` and `IReadWriteRepository<TEntity, TKey>` methods now take an optional `IClientSessionHandle session = null` parameter, inserted **before** `CancellationToken cancellationToken = default`. Consumers must recompile against 11.0.0 — this is not a binary-compatible drop-in for 10.x.
- Verified against `MongoDB.Driver` 3.8.0. Consumers pinning an older driver may need to upgrade; the session-aware code paths use driver members (`AsQueryable(IClientSessionHandle)`, `Collection.Find(session, ...)`, etc.) that are present in 3.x but may behave differently across patch versions.

#### Migration

- **Named-argument callers are unaffected.** `await repo.Get(id, cancellationToken: ct)` continues to compile.
- **Positional callers that passed the `CancellationToken` as the second argument must switch to named:** `await repo.Get(id, ct)` → `await repo.Get(id, cancellationToken: ct)`. Same for `Delete(id, ct)`, `Add(entity, options, ct)`, etc.
- **External implementers / mocks of the repository interfaces must add the new parameter** to every overridden method signature; the compiler will surface the missing-member errors.
- **Reads inside a transaction bypass the read-only collection.** When a session is passed to `Get`/`GetAll`/`Count`, the call routes to the read/write collection because the session is client-bound. If your deployment uses separate read-only and read/write connection strings, transactional reads will not be served by read replicas — this is required for correctness and intentional.
- **`Add` (and the other mutation methods) trim the in-memory entity's string properties before writing.** This is unchanged from earlier versions, but worth noting alongside the new transaction semantics: if a transactional `Add` is rolled back (or the driver retries `ExecuteInTransactionAsync` on a transient error), the caller's entity object remains mutated — the trim is not undone.

#### Known limitations

- The `[Obsolete]` LINQ-expression overloads (`Get<TProperty>(Expression<bool>)`, `GetAll<TProperty>(Expression<bool>, ...)`, `GetAllDescending<TProperty>(Expression<bool>, ...)`) were left without a session parameter. They are still present in 11.0.0 but slated for removal in v12; migrate to the non-obsolete equivalents if you need them inside a transaction.
- The test suite targets `net10.0` only. Production projects target `net10.0;net9.0;net8.0;netstandard2.1` and are compile-verified for all four; runtime test coverage on the older TFMs is not part of this release.

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
