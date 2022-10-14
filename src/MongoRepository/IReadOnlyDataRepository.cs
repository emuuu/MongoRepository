using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

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
        ConfiguredTaskAwaitable<TEntity> Get(TKey id, CancellationToken cancellationToken = default);

        /// <summary>	Gets all entities using the given identifiers. </summary>
        /// <param name="ids">	The Identifier to use. </param>
        /// <returns>	A TEntity. </returns>
        ConfiguredTaskAwaitable<List<TEntity>> Get(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <returns>	A TEntity. </returns>
        ConfiguredTaskAwaitable<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
        /// <returns>	A TEntity. </returns>
        ConfiguredTaskAwaitable<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);


        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition = null, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
        /// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
        /// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
		ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection in asynchronously. </summary>
		/// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
		ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
		ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
        /// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
		ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
        /// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<long> Count(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
        /// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<long> Count(string jsonFilterDefinition, CancellationToken cancellationToken = default);

        /// <summary>	Gets all items in this collection asynchronously. </summary>
        /// <param name="filter">	A linq expression to filter the results. </param>
        /// <param name="sorting">	A linq expression to sort the results.</param>
        /// <param name="page">	The requested page number. </param>
        /// <param name="pageSize">	The number of items per page.</param>
        /// <returns>
        ///     An list that allows foreach to be used to process all items in this collection.
        /// </returns>
        ConfiguredTaskAwaitable<long> Count<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
	}
}
