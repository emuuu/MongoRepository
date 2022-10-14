using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MongoRepository
{
	/// <summary>	A mongoDB read only repository. </summary>
	/// <typeparam name="TEntity">	Type of the entity. </typeparam>
	/// <typeparam name="TKey">   	Type of the key. </typeparam>
	public abstract class ReadOnlyDataRepository<TEntity, TKey> : IReadOnlyDataRepository<TEntity, TKey>
		where TEntity : class, IEntity<TKey>, new()
	{
		/// <summary>   Constructor. </summary>
		/// <param name="mongoOptions">   The mongoDB connection options. </param>
		protected ReadOnlyDataRepository(IOptions<MongoDbOptions> mongoOptions)
		{
			var context = new EntityContext<TEntity>(mongoOptions);
			Collection = context.Collection(true);
		}

		/// <summary>   Gets the mongoDB collection. </summary>
		/// <value> The mongoDB collection. </value>
		public virtual IMongoCollection<TEntity> Collection { get; }


		/// <summary>	Gets a t entity using the given identifier asynchronously. </summary>
		/// <param name="id">	The Identifier to get. </param>
		/// <returns>	A TEntity. </returns>
		public virtual ConfiguredTaskAwaitable<TEntity> Get(TKey id, CancellationToken cancellationToken = default)
		{
			var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
            return Collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets a t entity using the given identifier asynchronously. </summary>
		/// <param name="id">	The Identifier to get. </param>
		/// <returns>	A TEntity. </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> Get(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
		{
			var filter = Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids);
			return Collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
		/// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
		/// <returns>	A TEntity. </returns>
		public virtual ConfiguredTaskAwaitable<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default)
		{
            return Collection
                .Find(filterDefinition ?? new BsonDocument())
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets first item in this collection matching a given filter asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <returns>	A TEntity. </returns>
		public virtual ConfiguredTaskAwaitable<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
		{
            return Collection
                .AsQueryable()
				.Where(filter)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}


		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default)
		{
			return Collection
				.Find(filterDefinition ?? new BsonDocument())
				.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
		/// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition, CancellationToken cancellationToken = default)
		{
            return Collection
                .Find(filterDefinition ?? new BsonDocument())
				.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
				.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
		/// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition = null, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

				return Collection
				.Find(filterDefinition ?? new BsonDocument())
				.Skip((page - 1) * pageSize)
				.Limit(pageSize)
				.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
				.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                .Find(filterDefinition ?? new BsonDocument())
				.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
				.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}


		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}

			return GetAll(filterDefinition: filter, cancellationToken);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
		/// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}

			JsonSortDefinition<TEntity> sorting = null;
			if (!string.IsNullOrEmpty(jsonSortingDefinition))
			{
				sorting = new JsonSortDefinition<TEntity>(jsonSortingDefinition);
			}

			return GetAll(filterDefinition: filter, sortDefinition: sorting, cancellationToken);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
		/// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}

			JsonSortDefinition<TEntity> sorting = null;
			if (!string.IsNullOrEmpty(jsonSortingDefinition))
			{
				sorting = new JsonSortDefinition<TEntity>(jsonSortingDefinition);
			}

			return GetAll(filterDefinition: filter, sortDefinition: sorting, page: page, pageSize: pageSize, cancellationToken);
		}


		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
					.Where(filter)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>	Gets all items in this collection in asynchronously. </summary>
		/// <param name="sorting">	A linq expression to sort the results.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
					.Where(filter)
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}


		/// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
					.Where(filter)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
		/// <param name="sorting">	A linq expression to sort the results.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>	Gets all items in this collection in descending order asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
		{
			if (page.HasValue && pageSize.HasValue)
			{
				if (page < 1)
				{
					page = 1;
				}
				if (pageSize < 1)
				{
					pageSize = 1;
				}

                return Collection
                    .AsQueryable()
                    .Where(filter)
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
                return Collection
                    .AsQueryable()
                    .Where(filter)
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken).ConfigureAwait(false);
			}
		}


		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filterDefinition">	A definition to filter the results. Defaults to an empty filter.</param>
		/// <param name="sortDefinition">	The sorting definition for the result. Defaults to sort ascending by Id.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<long> Count(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default)
		{
            return Collection
				.Find(filterDefinition ?? new BsonDocument())
				.CountDocumentsAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="jsonFilterDefinition">	A definition to filter in a json string the results. Defaults to an empty filter.</param>
		/// <param name="jsonSortingDefinition">	The sorting definition in a json string for the result. Defaults to sort ascending by Id.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<long> Count(string jsonFilterDefinition, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}
			return Count(filterDefinition: filter, cancellationToken);
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <param name="filter">	A linq expression to filter the results. </param>
		/// <param name="sorting">	A linq expression to sort the results.</param>
		/// <param name="page">	The requested page number. </param>
		/// <param name="pageSize">	The number of items per page.</param>
		/// <returns>
		///     An list that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual ConfiguredTaskAwaitable<long> Count<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
		{
            return Collection
				.AsQueryable()
				.Where(filter)
				.LongCountAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
