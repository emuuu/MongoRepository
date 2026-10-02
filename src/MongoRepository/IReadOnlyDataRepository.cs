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
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>
        /// The entity with the given ID, or null if not found. Null is also returned when
        /// <paramref name="id"/> cannot be serialised into the stored key representation
        /// (e.g. a string that is not a valid 24-digit ObjectId) — such a key can never match a document.
        /// </returns>
        /// <exception cref="MongoException">Thrown when the query fails.</exception>
        /// <exception cref="FormatException">
        /// Thrown when a matched document cannot be deserialised into <typeparamref name="TEntity"/>,
        /// for example after schema drift where a stored field no longer matches the C# class.
        /// Such documents are reported as an error, not silently as "not found".
        /// </exception>
        Task<TEntity> Get(TKey id, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of entities matching the given IDs.
        /// </summary>
        /// <param name="ids">The IDs of the entities to retrieve.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>
        /// A list of matching entities. An empty list is returned when any of the supplied
        /// <paramref name="ids"/> cannot be serialised into the stored key representation;
        /// the whole query is rejected in that case, including the well-formed keys.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when a matched document cannot be deserialised into <typeparamref name="TEntity"/>,
        /// for example after schema drift where a stored field no longer matches the C# class.
        /// Such documents are reported as an error, not silently dropped from the result.
        /// </exception>
        Task<List<TEntity>> Get(IEnumerable<TKey> ids, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first entity matching the provided MongoDB filter definition.
        /// </summary>
        /// <param name="filterDefinition">A MongoDB filter definition.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The first matching entity, or null if none found.</returns>
        Task<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first entity matching the given LINQ expression filter.
        /// </summary>
        /// <typeparam name="TProperty">An arbitrary property type (not used directly).</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The first matching entity, or null if none found.</returns>
        [Obsolete("Use Get(FilterDefinition) or the LINQ Where().FirstOrDefault() pattern instead. The TProperty parameter is unused. This method will be removed in v13.")]
        Task<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of entities.</returns>
        Task<List<TEntity>> GetAll(IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities with optional filtering, sorting, and paging.
        /// </summary>
        /// <param name="filterDefinition">Optional filter definition.</param>
        /// <param name="sortDefinition">Optional sort definition.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of entities based on the provided criteria.</returns>
        /// <remarks>Page numbers less than 1 will default to 1.</remarks>
        Task<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities using JSON filter and sort definitions with paging.
        /// </summary>
        /// <param name="jsonFilterDefinition">JSON filter as a string.</param>
        /// <param name="jsonSortingDefinition">JSON sort as a string.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of paged, sorted, and filtered entities.</returns>
        Task<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition = null, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching a LINQ predicate with optional paging.
        /// </summary>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of filtered and optionally paged entities.</returns>
        /// <remarks>
        /// <para>
        /// No sort is applied; the documents come back in the server's natural order. Use
        /// <see cref="GetAll{TProperty}(Expression{Func{TEntity, bool}}, Expression{Func{TEntity, TProperty}}, int?, int?, IClientSessionHandle, CancellationToken)"/>
        /// for a stable order, in particular when paging. Paging applies only when both
        /// <paramref name="page"/> and <paramref name="pageSize"/> are given; values less than 1 default to 1.
        /// </para>
        /// <para>
        /// A lambda returning <see cref="bool"/>, such as <c>GetAll(x =&gt; x.IsActive)</c>, binds to this
        /// overload. Before 12.3.0 the same call bound to the sorting overload with <c>TProperty = bool</c>
        /// and returned every document, sorted by the predicate.
        /// </para>
        /// <para>
        /// The default implementation serves classes that implement this interface directly and were written
        /// before the member existed. It delegates to
        /// <see cref="GetAll(FilterDefinition{TEntity}, SortDefinition{TEntity}, int?, int?, IClientSessionHandle, CancellationToken)"/>
        /// with a <c>null</c> sort definition, so the order is whatever that implementation applies by default.
        /// <see cref="ReadOnlyDataRepository{TEntity, TKey}"/> implements the member itself and applies no sort.
        /// </para>
        /// </remarks>
        Task<List<TEntity>> GetAll(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
            => GetAll(filterDefinition: Builders<TEntity>.Filter.Where(filter), sortDefinition: null, page: page, pageSize: pageSize, session: session, cancellationToken: cancellationToken);

        /// <summary>
        /// Gets all entities matching a LINQ filter with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">An arbitrary property type (not used directly).</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of filtered and optionally paged entities.</returns>
        /// <remarks>
        /// Reachable only with an explicit type argument: <typeparamref name="TProperty"/> cannot be inferred,
        /// so a call without one binds to
        /// <see cref="GetAll(Expression{Func{TEntity, bool}}, int?, int?, IClientSessionHandle, CancellationToken)"/>.
        /// </remarks>
        [Obsolete("Use GetAll(FilterDefinition, SortDefinition, page, pageSize) instead. The TProperty parameter is unused. This method will be removed in v13.")]
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities sorted by the given expression with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to sort by.</typeparam>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of sorted and optionally paged entities.</returns>
        /// <remarks>
        /// A lambda returning <see cref="bool"/> passed positionally does not bind here; it binds to the
        /// predicate overload <see cref="GetAll(Expression{Func{TEntity, bool}}, int?, int?, IClientSessionHandle, CancellationToken)"/>
        /// and filters. To sort by a boolean member, name the argument — <c>GetAll(sorting: x =&gt; x.IsActive)</c> —
        /// or use <c>GetAll(FilterDefinition&lt;TEntity&gt;.Empty, Builders&lt;TEntity&gt;.Sort.Ascending(x =&gt; x.IsActive))</c>.
        /// An explicit type argument alone, <c>GetAll&lt;bool&gt;(x =&gt; x.IsActive)</c>, does not compile until v13:
        /// it is ambiguous with the obsolete <see cref="GetAll{TProperty}(Expression{Func{TEntity, bool}}, int?, int?, CancellationToken)"/>.
        /// </remarks>
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching the filter and sorted by the given expression with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to sort by.</typeparam>
        /// <param name="filter">The LINQ filter expression.</param>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A list of filtered, sorted, and optionally paged entities.</returns>
        Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Not supported: a descending order needs a sorting expression. Use
        /// <see cref="GetAllDescending{TProperty}(Expression{Func{TEntity, bool}}, Expression{Func{TEntity, TProperty}}, int?, int?, IClientSessionHandle, CancellationToken)"/>.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="session">Optional session for transactional reads.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Never returns.</returns>
        /// <remarks>
        /// This overload exists so that a lambda returning <see cref="bool"/>, such as
        /// <c>GetAllDescending(x =&gt; x.IsActive)</c>, binds here and the compiler reports an error. Before
        /// 12.3.0 the same call bound to the sorting overload with <c>TProperty = bool</c> and returned every
        /// document, sorted by the predicate. To sort by a boolean member, name the argument:
        /// <c>GetAllDescending(sorting: x =&gt; x.IsActive)</c>.
        /// </remarks>
        /// <exception cref="NotSupportedException">Always.</exception>
        [Obsolete("GetAllDescending cannot sort without a sorting expression; use GetAllDescending(filter, sorting) instead. To sort by a boolean member, name the argument: GetAllDescending(sorting: x => x.Flag).", error: true)]
        Task<List<TEntity>> GetAllDescending(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("GetAllDescending cannot sort without a sorting expression; use GetAllDescending(filter, sorting) instead.");

        /// <summary>
        /// Gets all entities matching the filter in descending order with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="filter">The filter expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of sorted entities.</returns>
        /// <remarks>
        /// Reachable only with an explicit type argument: <typeparamref name="TProperty"/> cannot be inferred,
        /// so a call without one binds to the non-generic overload, which the compiler rejects.
        /// </remarks>
        [Obsolete("This method cannot sort without a sorting expression. Use GetAllDescending(filter, sorting) instead. This method will be removed in v13.")]
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities sorted in descending order by the given property with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of sorted entities.</returns>
        /// <remarks>
        /// A lambda returning <see cref="bool"/> passed positionally does not bind here; it binds to an
        /// overload the compiler rejects. To sort by a boolean member, name the argument:
        /// <c>GetAllDescending(sorting: x =&gt; x.IsActive)</c>. An explicit type argument alone,
        /// <c>GetAllDescending&lt;bool&gt;(x =&gt; x.IsActive)</c>, does not compile until v13: it is ambiguous with the
        /// obsolete <see cref="GetAllDescending{TProperty}(Expression{Func{TEntity, bool}}, int?, int?, CancellationToken)"/>.
        /// </remarks>
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching the filter and sorts them in descending order by the given property with optional paging.
        /// </summary>
        /// <typeparam name="TProperty">The property to sort by.</typeparam>
        /// <param name="filter">The filter expression.</param>
        /// <param name="sorting">The sorting expression.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the read targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of filtered and sorted entities.</returns>
        Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the filter definition.
        /// </summary>
        /// <param name="filterDefinition">The MongoDB filter definition.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the count targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(FilterDefinition<TEntity> filterDefinition = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the JSON filter.
        /// </summary>
        /// <param name="jsonFilterDefinition">The filter in JSON format.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the count targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(string jsonFilterDefinition, IClientSessionHandle session = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of documents matching the LINQ filter.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="session">Optional session for transactional reads. When supplied, the count targets the read/write collection.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of matching documents.</returns>
        Task<long> Count(Expression<Func<TEntity, bool>> filter, IClientSessionHandle session = null, CancellationToken cancellationToken = default);
    }
}
