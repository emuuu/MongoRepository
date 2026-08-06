using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository
{
    public abstract class ReadOnlyDataRepository<TEntity, TKey> : IReadOnlyDataRepository<TEntity, TKey>
		where TEntity : class, IEntity<TKey>, new()
	{
		protected ReadOnlyDataRepository(IOptions<MongoDbOptions> mongoOptions)
		{
			_context = new EntityContext<TEntity>(mongoOptions);
			Collection = _context.Collection(true);
		}

		private protected readonly EntityContext<TEntity> _context;

		public virtual IMongoCollection<TEntity> Collection { get; }

		/// <summary>
		/// When a session is provided, reads must execute against the read/write
		/// collection because sessions are bound to the read/write client; otherwise
		/// the driver throws <see cref="InvalidOperationException"/>.
		/// </summary>
		private IMongoCollection<TEntity> CollectionFor(IClientSessionHandle session)
			=> session is null ? Collection : _context.Collection(false);

		/// <summary>
		/// Renders a key-based filter to BSON before the query runs, so that a key
		/// that cannot be serialised (e.g. a string that is not a valid 24-digit
		/// ObjectId) is detected up front. Returns <c>null</c> in that case.
		/// </summary>
		/// <remarks>
		/// Only the render step is guarded. Executing the query and deserialising
		/// its documents stay outside the try/catch on purpose: a
		/// <see cref="FormatException"/> raised while materialising a stored
		/// document — schema drift, where a persisted field no longer matches the
		/// C# class — must surface to the caller instead of being reported as
		/// "not found".
		/// </remarks>
		private static FilterDefinition<TEntity> RenderKeyFilter(FilterDefinition<TEntity> filter, IMongoCollection<TEntity> collection)
		{
			try
			{
				return filter.Render(new RenderArgs<TEntity>(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
			}
			catch (FormatException)
			{
				// Invalid BsonId format (e.g., invalid ObjectId string)
				return null;
			}
			catch (ArgumentException)
			{
				// Invalid argument for ID conversion
				return null;
			}
		}

		public virtual async Task<TEntity> Get(TKey id, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var filter = RenderKeyFilter(Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id), collection);
			if (filter is null)
				return null;

			var cursor = session is null ? collection.Find(filter) : collection.Find(session, filter);
			return await cursor.FirstOrDefaultAsync(cancellationToken);
		}

		public virtual async Task<List<TEntity>> Get(IEnumerable<TKey> ids, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var filter = RenderKeyFilter(Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids), collection);
			if (filter is null)
				return new List<TEntity>();

			var cursor = session is null ? collection.Find(filter) : collection.Find(session, filter);
			return await cursor.ToListAsync(cancellationToken);
		}

		public virtual Task<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var filter = filterDefinition ?? new BsonDocument();
			return (session is null ? collection.Find(filter) : collection.Find(session, filter))
				.FirstOrDefaultAsync(cancellationToken);
		}

		[Obsolete("Use Get(FilterDefinition) or the LINQ Where().FirstOrDefault() pattern instead. The TProperty parameter is unused. This method will be removed in v13.")]
		public virtual Task<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
		{
            return Collection
                .AsQueryable()
				.Where(filter)
				.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual Task<List<TEntity>> GetAll(IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
			var collection = CollectionFor(session);
			var filter = Builders<TEntity>.Filter.Empty;
			return (session is null ? collection.Find(filter) : collection.Find(session, filter))
				.ToListAsync(cancellationToken);
        }

		public virtual Task<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var filter = filterDefinition ?? new BsonDocument();
			var find = session is null ? collection.Find(filter) : collection.Find(session, filter);

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

				return find
					.Skip((page - 1) * pageSize)
					.Limit(pageSize)
					.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
					.ToListAsync(cancellationToken);
			}
			else
			{
				return find
					.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition = null, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
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

			return GetAll(filterDefinition: filter, sortDefinition: sorting, page: page, pageSize: pageSize, session: session, cancellationToken: cancellationToken);
		}

		[Obsolete("Use GetAll(FilterDefinition, SortDefinition, page, pageSize) instead. The TProperty parameter is unused. This method will be removed in v13.")]
		public virtual Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var queryable = session is null ? collection.AsQueryable() : collection.AsQueryable(session);

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

				return queryable
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
			else
			{
				return queryable
					.OrderBy(sorting)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var queryable = session is null ? collection.AsQueryable() : collection.AsQueryable(session);

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

				return queryable
					.Where(filter)
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
			else
			{
				return queryable
					.Where(filter)
					.OrderBy(sorting)
					.ToListAsync(cancellationToken);
			}
		}


		[Obsolete("This method cannot sort without a sorting expression. Use GetAllDescending(filter, sorting) instead. This method will be removed in v13.")]
		public virtual Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var queryable = session is null ? collection.AsQueryable() : collection.AsQueryable(session);

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

				return queryable
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
			else
			{
				return queryable
					.OrderByDescending(sorting)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var queryable = session is null ? collection.AsQueryable() : collection.AsQueryable(session);

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

				return queryable
					.Where(filter)
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
			else
			{
				return queryable
					.Where(filter)
					.OrderByDescending(sorting)
					.ToListAsync(cancellationToken);
			}
		}


		public virtual Task<long> Count(FilterDefinition<TEntity> filterDefinition = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var filter = filterDefinition ?? new BsonDocument();
			return (session is null ? collection.Find(filter) : collection.Find(session, filter))
				.CountDocumentsAsync(cancellationToken);
		}

		public virtual Task<long> Count(string jsonFilterDefinition, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}
			return Count(filterDefinition: filter, session: session, cancellationToken: cancellationToken);
		}

		public virtual Task<long> Count(Expression<Func<TEntity, bool>> filter, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
		{
			var collection = CollectionFor(session);
			var queryable = session is null ? collection.AsQueryable() : collection.AsQueryable(session);
			return queryable
				.Where(filter)
				.LongCountAsync(cancellationToken);
		}
	}
}
