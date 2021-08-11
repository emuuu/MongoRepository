using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
        Task<TEntity> Add(TEntity entity);

        /// <summary>	Adds a range of entities asynchronously. </summary>
        /// <param name="entities">	An IEnumerable&lt;TEntity&gt; of items to append to this collection. </param>
        Task AddRange(IEnumerable<TEntity> entities);

        /// <summary>	Updates the given entity asynchronously. </summary>
        /// <param name="entity">	The entity to add. </param>
        /// <returns>	A TEntity. </returns>
        Task<TEntity> Update(TEntity entity);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="id">	The Identifier to delete. </param>
        Task Delete(TKey id);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filterDefinition">	A definition to filter which documents will be deleted. Defaults to an empty filter.</param>
        Task Delete(FilterDefinition<TEntity> filterDefinition);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string which documents will be deleted. Defaults to an empty filter.</param>
        Task Delete(string jsonFilterDefinition);

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filter">	A linq expression to filter which documents will be deleted. </param>
        Task Delete(Expression<Func<TEntity, bool>> filter);
    }
}
