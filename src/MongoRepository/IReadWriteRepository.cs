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
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        /// <exception cref="MongoException">Thrown if the insert fails.</exception>
        Task Add(TEntity entity, InsertOneOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple entities into the database.
        /// All string properties are trimmed before insertion.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="options">Optional insert many options.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous insert operation.</returns>
        Task AddRange(IEnumerable<TEntity> entities, InsertManyOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an existing entity in the database by ID.
        /// All string properties are trimmed before the replacement.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="replaceOptions">Optional replace options.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous replace operation, with the result.</returns>
        Task<ReplaceOneResult> Update(TEntity entity, ReplaceOptions replaceOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a bulk update of multiple entities.
        /// All string properties are trimmed before the update.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="bulkWriteOptions">Optional bulk write options.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous bulk write operation, with the result.</returns>
        Task<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, BulkWriteOptions bulkWriteOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a single entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple entities by their IDs.
        /// </summary>
        /// <param name="ids">The IDs of the entities to delete.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all entities matching the given MongoDB filter definition.
        /// </summary>
        /// <param name="filterDefinition">The MongoDB filter definition.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all entities matching the given LINQ expression filter.
        /// </summary>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
    }
}
