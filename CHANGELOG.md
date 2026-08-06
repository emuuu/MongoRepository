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

### [12.0.0]

#### Changed

- **Breaking — `Get(TKey)` and `Get(IEnumerable<TKey>)` no longer swallow deserialization failures.** Both methods used to wrap the entire operation — filter construction, query execution *and* document deserialization — in a `catch (FormatException)` / `catch (ArgumentException)` that returned `null` / an empty list. The comment claimed the catch was there for an invalid `BsonId` format, but it also caught every `FormatException` raised while materialising a fetched document. The practical effect: after schema drift, where a stored field no longer matches the C# property (for example a `Value` persisted as a string against an `int` property), the affected documents were reported as "not found" and the actual cause never surfaced. Those exceptions now propagate to the caller.
- The invalid-key behaviour is preserved, but is now decided before the query runs. The id filter is rendered to BSON up front; only that render step is guarded. A key that cannot be serialized into its stored representation — the canonical case being a string that is not a valid 24-digit ObjectId against a `[BsonRepresentation(BsonType.ObjectId)]` key — still yields `null` from `Get(TKey)` and an empty list from `Get(IEnumerable<TKey>)`, because such a key can never match a document. Executing the query and deserializing its results are outside the guard.
- `Get(IEnumerable<TKey>)` continues to reject the whole query — returning an empty list — when *any* supplied key is unserializable, including the well-formed ones. This is unchanged from 11.x and is now stated explicitly in the XML docs rather than being an accident of the catch placement.
- **Removal of the `[Obsolete]` LINQ-expression overloads is deferred to v13.** Their attribute messages still announced removal in v11 while 11.0.0 shipped with them in place; the 11.0.0 notes then named v12. Both are now corrected to v13, so the attribute text and the release plan agree. The overloads (`Get<TProperty>(Expression<bool>)`, `GetAll<TProperty>(Expression<bool>, ...)`, `GetAllDescending<TProperty>(Expression<bool>, ...)`) remain present and unchanged in 12.0.0. They still take no session parameter — migrate to the non-obsolete equivalents if you need them inside a transaction.

#### Added

- Tests covering the two paths separately: an unserializable key still returns `null` / an empty list, and a document that cannot be deserialized now throws instead of being reported as missing. The schema-drift fixture writes through the raw `BsonDocument` collection and asserts the document is genuinely present, so a passing test cannot be explained by a missing document.

#### Migration

- **`Get(id)` returning `null` no longer means "no such document, or something failed to deserialize".** It now means only "no such document, or a key that cannot address one". Callers that treated `null` as a benign absence must be prepared for a `FormatException` where a document exists but cannot be mapped. Where that has to stay non-fatal — a degraded list view, a background sweep — catch it at the call site, log it, and keep the exception visible instead of restoring the blanket catch.
- **Expect previously invisible data problems to surface on upgrade.** Documents broken by earlier schema changes were being read as "not found"; after this release the same reads fail loudly. That is the point of the change, but it can turn a quiet collection into a noisy one at deploy time. Consider running a read sweep over the affected collections in a staging environment first.
- **Consumers asserting the old behaviour must update their tests.** A test that seeds a document with a legacy field and asserts `Get` returns `null` was pinning the 11.x masking behaviour. That assertion inverts with this release — the call now throws — and has to be rewritten to expect the exception.
- **No signature changes.** Unlike 11.0.0, this release does not alter any method signature; the break is purely behavioural, so the compiler will not point at the affected call sites.

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
