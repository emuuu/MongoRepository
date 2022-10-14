using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MongoRepository
{
    /// <summary>	Interface for a data repository. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    /// <typeparam name="TKey">   	Type of the key. </typeparam>
    public interface IReadWriteRepository<TEntity, TKey> : IReadOnlyDataRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>	Adds entity asynchronously. </summary>
        /// <param name="entity">	The entity to add. </param>
        /// <returns>	A TEntity. </returns>
        ConfiguredTaskAwaitable Add(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>	Adds a range of entities asynchronously. </summary>
        /// <param name="entities">	An IEnumerable&lt;TEntity&gt; of items to append to this collection. </param>
        ConfiguredTaskAwaitable AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>	Updates the given entity asynchronously. </summary>
        /// <param name="entity">	The entity to add. </param>
        /// <returns>	A TEntity. </returns>
        ConfiguredTaskAwaitable<ReplaceOneResult> Update(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>	Updates a range of entities asynchronously. </summary>
        /// <param name="entities">	An IEnumerable&lt;TEntity&gt; of items to be updated in this collection. </param>
        ConfiguredTaskAwaitable<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="id">	The Identifier to delete. </param>
        ConfiguredTaskAwaitable<DeleteResult> Delete(TKey id, CancellationToken cancellationToken = default);

        /// <summary>	Deletes the given IDs. </summary>
        /// <param name="ids">	The Identifiers to delete. </param>
        ConfiguredTaskAwaitable<DeleteResult> Delete(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filterDefinition">	A definition to filter which documents will be deleted. Defaults to an empty filter.</param>
        ConfiguredTaskAwaitable<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filter">	A linq expression to filter which documents will be deleted. </param>
        ConfiguredTaskAwaitable<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
    }
}
