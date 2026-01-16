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
			var context = new EntityContext<TEntity>(mongoOptions);
			Collection = context.Collection(true);
		}

		public virtual IMongoCollection<TEntity> Collection { get; }


		public virtual async Task<TEntity> Get(TKey id, CancellationToken cancellationToken = default)
		{
			try
			{
				var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
				return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
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

		public virtual async Task<List<TEntity>> Get(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
		{
			try
			{
				var filter = Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids);
				return await Collection.Find(filter).ToListAsync(cancellationToken);
			}
			catch (FormatException)
			{
				return new List<TEntity>();
			}
			catch (ArgumentException)
			{
				return new List<TEntity>();
			}
		}

		public virtual Task<TEntity> Get(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default)
		{
            return Collection
                .Find(filterDefinition ?? new BsonDocument())
				.FirstOrDefaultAsync(cancellationToken);
		}

		public virtual Task<TEntity> Get<TProperty>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
		{
            return Collection
                .AsQueryable()
				.Where(filter)
				.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual Task<List<TEntity>> GetAll(CancellationToken cancellationToken = default)
        {
            return Collection
				.Find(Builders<TEntity>.Filter.Empty)
                .ToListAsync(cancellationToken);
        }

		public virtual Task<List<TEntity>> GetAll(FilterDefinition<TEntity> filterDefinition, SortDefinition<TEntity> sortDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
				.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                .Find(filterDefinition ?? new BsonDocument())
				.Sort(sortDefinition ?? Builders<TEntity>.Sort.Ascending(nameof(IEntity<TKey>.Id)))
				.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAll(string jsonFilterDefinition, string jsonSortingDefinition = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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

		public virtual Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAll<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.Where(filter)
					.OrderBy(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
		}


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
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
		}

		public virtual Task<List<TEntity>> GetAllDescending<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> sorting, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
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
					.ToListAsync(cancellationToken);
			}
			else
			{
                return Collection
                    .AsQueryable()
                    .Where(filter)
					.OrderByDescending(sorting)
					.Skip((page.Value - 1) * pageSize.Value)
					.Take(pageSize.Value)
					.ToListAsync(cancellationToken);
			}
		}


		public virtual Task<long> Count(FilterDefinition<TEntity> filterDefinition = null, CancellationToken cancellationToken = default)
		{
            return Collection
				.Find(filterDefinition ?? new BsonDocument())
				.CountDocumentsAsync(cancellationToken);
		}

		public virtual Task<long> Count(string jsonFilterDefinition, CancellationToken cancellationToken = default)
		{
			JsonFilterDefinition<TEntity> filter = null;
			if (!string.IsNullOrEmpty(jsonFilterDefinition))
			{
				filter = new JsonFilterDefinition<TEntity>(jsonFilterDefinition);
			}
			return Count(filterDefinition: filter, cancellationToken);
		}

		public virtual Task<long> Count(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
		{
            return Collection
				.AsQueryable()
				.Where(filter)
				.LongCountAsync(cancellationToken);
		}
	}
}
