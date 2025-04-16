using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>
    /// Defines read-only operations for a MongoDB-backed data repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's unique identifier.</typeparam>
    public interface IReadOnlyDataRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>
        /// Gets a single entity by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The entity with the given ID, or null if not found.</returns>
        /// <exception cref="MongoException">Thrown when the query fails.</exception>
        Task<TEntity> Get(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of entities matching the given IDs.
        /// </summary>
        /// <param name="ids">The IDs of the entities to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of matching entities.</returns>
        Task<List<TEntity>> Get(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first entity matching the provided MongoDB filter definition.
        /// </summary>
        /// <param name="filterDefinition">A MongoDB filter definition.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The first matching entity, or null if none found.</returns>
        Task<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first entity matching the given LINQ expression filter.
        /// </summary>
        /// <typeparam name="TProperty">An arbitrary property type (not used directly).</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The first matching entity, or null if none found.</returns>
        Task<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <returns>A list of entities.</returns>
        Task<List<TEntity>> GetAll(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities with optional filtering, sorting, and paging.
        /// </summary>
        /// <param name="filterDefinition">Optional filter definition.</param>
        /// <param name="sortDefinition">Optional sort definition.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of entities based on the provided criteria.</returns>
        /// <remarks>Page numbers less than 1 will default to 1.</remarks>
        Task<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities using JSON filter and sort definitions with paging.
        /// </summary>
        /// <param name="jsonFilterDefinition">JSON filter as a string.</param>
        /// <param name="jsonSortingDefinition">JSON sort as a string.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of paged, sorted, and filtered entities.</returns>
        Task<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching a LINQ filter with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">An arbitrary property type (not used directly).</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of filtered and optionally paged entities.</returns>
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities sorted by the given expression with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to sort by.</typeparam>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of sorted and optionally paged entities.</returns>
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching the filter and sorted by the given expression with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to sort by.</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of filtered, sorted, and optionally paged entities.</returns>
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching the filter in descending order with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="filter">The filter expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of sorted entities.</returns>
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities sorted in descending order by the given property with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of sorted entities.</returns>
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching the filter and sorts them in descending order by the given property with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="filter">The filter expression.</param>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of filtered and sorted entities.</returns>
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the filter definition.
        /// </summary>
        /// <param name="filterDefinition">The MongoDB filter definition.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the JSON filter.
        /// </summary>
        /// <param name="jsonFilterDefinition">The filter in JSON format.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(string jsonFilterDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the LINQ filter.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
    }
}
