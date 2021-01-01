using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>	Interface for a read only data repository. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    /// <typeparam name="TKey">   	Type of the key. </typeparam>
    public interface IReadOnlyDataRepository<TEntity, in TKey> : IRepository
            where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>	Gets an entity using the given identifier. </summary>
        /// <param name="id">	The Identifier to use. </param>
        /// <returns>	A TEntity. </returns>
        Task<TEntity> Get(TKey id);

        /// <summary>	Gets all entities using the given identifiers. </summary>
        /// <param name="ids">	The Identifier to use. </param>
        /// <returns>	A TEntity. </returns>
        Task<IList<TEntity>> Get(IEnumerable<TKey> ids);

        /// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <returns>	A TEntity. </returns>
        Task<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null);

        /// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
        /// <returns>	A TEntity. </returns>
        Task<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        Task<IList<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition = null, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
        /// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        Task<IList<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition, int? page = null, int? pageSize = null);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
		Task<IList<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null);
    }
}
