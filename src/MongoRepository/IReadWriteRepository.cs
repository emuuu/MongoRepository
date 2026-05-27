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
        /// All string properties are trimmed before insertion.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="options">Optional insert options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        /// <exception cref="MongoException">Thrown if the insert fails.</exception>
        Task Add(TEntity entity, InsertOneOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple entities into the database.
        /// All string properties are trimmed before insertion.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="options">Optional insert many options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        Task AddRange(IEnumerable<TEntity> entities, InsertManyOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an existing entity in the database by ID.
        /// All string properties are trimmed before the replacement.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="replaceOptions">Optional replace options.</param>
        /// <param name="session">Optional session for transactional writes.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous replace operation, with the result.</returns>
        Task<ReplaceOneResult> Update(TEntity entity, ReplaceOptions replaceOptions = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a bulk update of multiple entities.
        /// All string properties are trimmed before the update.
        /// </summary>
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
        /// transactions (ReplicaSet or LoadBalanced topology). Returns <c>false</c>
        /// for standalone deployments.
        /// </summary>
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
