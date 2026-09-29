using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>
    /// Defines read and write operations for a MongoDB-backed data repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's unique identifier.</typeparam>
    public interface IReadWriteRepository<TEntity, TKey> : IReadOnlyDataRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>
        /// Inserts a single entity into the database.
        /// String properties are trimmed before insertion; the key is not.
        /// </summary>
        /// <remarks>
        /// <para>Trimming removes leading and trailing whitespace from every readable and writable
        /// <see cref="string"/> property of the entity that is not the key and carries neither
        /// <c>[BsonIgnore]</c> nor <see cref="NoTrimAttribute"/>. Null, empty and whitespace-only
        /// values are left as they are, and only the entity's own properties are affected — strings
        /// inside nested objects and collections are not trimmed.</para>
        /// <para>The key — <see cref="IEntity{TKey}.Id"/> and whichever member is stored as
        /// <c>_id</c> — is stored exactly as passed, so the same value finds the document again
        /// through <c>Get</c>, <c>Update</c> and <c>Delete</c>, none of which trims either.</para>
        /// <para>The entity is trimmed in place: the instance passed in carries the trimmed values
        /// afterwards, also when the insert fails or the surrounding transaction is rolled back.</para>
        /// </remarks>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="options">Optional insert options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        /// <exception cref="MongoException">Thrown if the insert fails.</exception>
        Task Add(TEntity entity, InsertOneOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple entities into the database.
        /// String properties are trimmed before insertion; the keys are not.
        /// </summary>
        /// <remarks>
        /// Trims each entity in place, by the rules described for
        /// <see cref="Add(TEntity, InsertOneOptions, IClientSessionHandle, CancellationToken)"/>.
        /// </remarks>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="options">Optional insert many options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        Task AddRange(IEnumerable<TEntity> entities, InsertManyOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an existing entity in the database by ID.
        /// String properties are trimmed before the replacement; the key is not.
        /// </summary>
        /// <remarks>
        /// Trims the entity in place, by the rules described for
        /// <see cref="Add(TEntity, InsertOneOptions, IClientSessionHandle, CancellationToken)"/>.
        /// The document is matched by the untouched key, so a key that carries surrounding
        /// whitespace still addresses the document stored under it — and an upsert does not
        /// create a second document under a trimmed key.
        /// </remarks>
        /// <param name="entity">The entity to update.</param>
        /// <param name="replaceOptions">Optional replace options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous replace operation, with the result.</returns>
        Task<ReplaceOneResult> Update(TEntity entity, ReplaceOptions replaceOptions = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a bulk update of multiple entities.
        /// String properties are trimmed before the update; the keys are not.
        /// </summary>
        /// <remarks>
        /// Trims each entity in place and matches it by its untouched key, as described for
        /// <see cref="Update(TEntity, ReplaceOptions, IClientSessionHandle, CancellationToken)"/>.
        /// </remarks>
        /// <param name="entities">The entities to update.</param>
        /// <param name="bulkWriteOptions">Optional bulk write options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous bulk write operation, with the result.</returns>
        Task<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, BulkWriteOptions bulkWriteOptions = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a single entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(TKey id, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple entities by their IDs.
        /// </summary>
        /// <param name="ids">The IDs of the entities to delete.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(IEnumerable<TKey> ids, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all entities matching the given MongoDB filter definition.
        /// </summary>
        /// <param name="filterDefinition">The MongoDB filter definition.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all entities matching the given LINQ expression filter.
        /// </summary>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a session on the read/write client. Sessions can be passed to the
        /// session-accepting overloads of the repository's read and write methods to
        /// scope operations to a transaction.
        /// </summary>
        /// <remarks>
        /// <see cref="IClientSessionHandle"/> implements <see cref="IDisposable"/>,
        /// not <see cref="IAsyncDisposable"/>; dispose it with <c>using var session = ...</c>.
        /// </remarks>
        Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns <c>true</c> when the underlying cluster supports multi-document
        /// transactions: ReplicaSet, Sharded, or LoadBalanced topology, or a
        /// direct connection (<c>directConnection=true</c>) to a replica set
        /// member or shard router. Returns <c>false</c> for standalone deployments
        /// and for any failure during the capability probe.
        /// </summary>
        /// <remarks>
        /// Each call performs a <c>ping</c> on the <c>admin</c> database to force
        /// server discovery; the result is not cached. Cluster topology can change
        /// at runtime (failover, reconfig), and a transient probe failure during
        /// such a window returns <c>false</c> — connection issues surface again on
        /// the next real operation, which is the right place to handle them.
        /// </remarks>
        Task<bool> SupportsTransactionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="work"/> inside a Mongo transaction, committing on
        /// success and aborting on exception. Internally uses the driver's
        /// <c>IClientSessionHandle.WithTransactionAsync</c>, which retries the
        /// transaction automatically on TransientTransactionError and
        /// UnknownTransactionCommitResult errors.
        /// </summary>
        /// <remarks>
        /// The work delegate may run more than once — keep it idempotent, do not
        /// trigger non-repeatable side effects (HTTP/messaging/file IO), and do
        /// not catch-and-suppress exceptions inside it. Every repository call
        /// invoked from within the delegate must pass the supplied session,
        /// otherwise it runs outside the transaction and breaks atomicity.
        /// </remarks>
        Task ExecuteInTransactionAsync(Func<IClientSessionHandle, CancellationToken, Task> work, CancellationToken cancellationToken = default);
    }
}
