# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## MongoGenericRepository / MongoGenericRepository.HealthChecks

### [12.2.0]

#### Fixed

- **The key is no longer trimmed on write.** `Add`, `AddRange` and both `Update` overloads trimmed every string property of the entity, the `Id` included. The document was then stored under a key the caller did not hold: a key built from input with surrounding whitespace — a natural key such as `"{eventId}:{address}"` — was written trimmed, and a later `Get` with the key as built found nothing. The read side, `Get`, `Delete` and the id filters, never trimmed, so the two sides disagreed. That also broke the usual recovery from a duplicate insert: the second `Add` failed with `DuplicateKey`, and the re-lookup with the same key missed the document that caused it. `Id`, and whichever member the serializer stores as `_id` (for example a differently named `[BsonId]` property), are now written exactly as passed.
- **`Update` matches a document whose key carries whitespace.** The entity was trimmed before the id filter was built, so `Update` filtered on the trimmed key: it missed a document stored under the untrimmed one — `MatchedCount` 0 — and with `IsUpsert` created a second document next to it. The filter now uses the untouched key. The same applies to every entity of the bulk `Update`.
- **`AddRange` and the bulk `Update` enumerate their sequence once.** Both walked the sequence to trim it and then again to write it. A lazy projection therefore ran twice: `AddRange(source.Select(x => new Entity { ... }))` inserted fresh, untrimmed instances, and a projection that mutates — `Update(items.Select(p => { p.Price *= 1.1m; return p; }))`, as in the documentation — applied the change twice.
- **Static properties are no longer trimmed.** The trim walked every public property of the entity type, static ones included, so a static string property with a setter was rewritten on every write — process-wide state changed by a repository call, although static members are not part of the stored document. Only instance properties are trimmed now.

#### Added

- **`[NoTrim]`** keeps a string property out of the trimming, for values whose surrounding whitespace carries meaning or has to round-trip unchanged: natural keys held outside the id, hashes, signatures, preformatted text.
- The XML documentation of `Add`, `AddRange` and both `Update` overloads spells out what is trimmed, what is not, and that the trim happens in place on the instance passed in — also when the write fails or a transaction is rolled back.

#### Changed

- **Behaviour change for keys with surrounding whitespace.** Nothing changes for keys without leading or trailing whitespace, generated ids included. Where a key does carry whitespace, it is now stored and matched as passed instead of trimmed, and the `Id` of the instance passed in keeps its whitespace after the call instead of being trimmed in place. Code that relied on the old trim to normalise keys behaves differently:
  - Documents written by earlier versions are stored under the trimmed key. An `Update` built from untrimmed input used to hit them and now misses them; with `IsUpsert` it creates a second document under the untrimmed key.
  - An `Add` with an untrimmed key followed by a `Get` with the trimmed form — for example from a path that normalises its input — used to find the document and now does not.
  - A key that is only valid once trimmed, such as an ObjectId string with surrounding whitespace against a `[BsonRepresentation(BsonType.ObjectId)]` key, now fails to serialise.

  Normalise keys before building them if that is what you want. To find documents affected by the new behaviour, look for pairs of keys that differ only in surrounding whitespace, and for updates that report `MatchedCount` 0 where a document was expected.

### [12.1.0]

#### Added

- Documentation site with API reference (Blazor WebAssembly + GitHub Pages)
- GitHub Actions CI/CD workflows
- Health check add-on package (`MongoGenericRepository.HealthChecks`)
- **Opt-in serialization conventions.** `MongoDbOptions.Serialization`, a new `MongoSerializationOptions`, switches on the driver's `IgnoreExtraElements` and `IgnoreIfNull` conventions without putting `[BsonIgnoreExtraElements]` on every entity class and `[BsonIgnoreIfNull]` on every nullable property. The motivation is schema evolution: during a rolling update, instances running the newer schema write fields the older ones do not declare, and a strict driver turns those documents into `FormatException`s. Both switches are independent `bool?` values that register nothing while they are unset, so an application that does not configure them keeps the behaviour of 12.0.0 exactly.
- **Scoping for the conventions.** `Namespaces` and `TypeFilter`, combined by AND, limit them to part of the type graph rather than the whole process. Namespaces match whole segments, so `MyApp.Orders` covers `MyApp.Orders.Archive` but not `MyApp.OrdersArchive`; `TypeFilter` can only be set from code, because the configuration binder skips delegate properties. Entries that are null, empty or whitespace are dropped; a `Namespaces` list that holds nothing usable is rejected with an `ArgumentException` instead of quietly widening the conventions to every type.
- **`MongoRepositoryConventions`**, the registration entry point. `Register` applies the given options and belongs at the start of application start-up; `Unregister` removes the pack again, for tests and for hosts that reconfigure themselves. Options are validated before the registry is touched, so a rejected call leaves an existing registration intact, and `ConventionPackName` may not take over the driver's own pack names `__defaults__` and `__attributes__`. Applications that never call `Register` get them from the first `EntityContext` constructed with options that ask for a convention, as a fallback; a context whose switches are all unset leaves the state open for a later one.
- **The limits of the conventions, each covered by a test.** They only reach class maps the driver builds after the registration, and a class map is built once per type and per process and never revisited — which is why the explicit call belongs before the first repository call. Attributes win over conventions: `[BsonIgnoreExtraElements(false)]` keeps an entity strict while a tolerant convention is registered, and `[BsonIgnoreExtraElements]` keeps it tolerant against a strict one, because the driver applies attribute conventions last. `IgnoreIfNull` changes the stored document rather than only the read path — a null property is absent instead of being stored as null, which `$exists` filters and sparse or partial indexes react to. The fallback registration happens once: later `MongoDbOptions` instances carrying different values do not change an existing registration.

#### Changed

- Tests run on Microsoft.Testing.Platform. A `global.json` at the repository root selects the runner, and the CI and release workflows now call `dotnet test --project <csproj>` from the repository root, because Microsoft.Testing.Platform, which `xunit.v3` brings in, refuses to run through the VSTest target on the .NET 10 SDK.

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
