using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

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
		public virtual async Task<TEntity> Get(TKey id)
		{
			var filter = Builders<TEntity>.Filter.Eq("Id", id);
			TEntity result = await Collection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
			return result;
		}

		/// <summary>	Gets a t entity using the given identifier asynchronously. </summary>
		/// <param name="id">	The Identifier to get. </param>
		/// <returns>	A TEntity. </returns>
		public virtual async Task<IList<TEntity>> Get(IEnumerable<TKey> ids)
		{
			var filter = Builders<TEntity>.Filter.In("Id", ids);
			IList<TEntity> result = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
			return result;
		}

		/// <summary>	Gets all items in this collection asynchronously. </summary>
		/// <returns>
		///     An enumerator that allows foreach to be used to process all items in this collection.
		/// </returns>
		public virtual async Task<IList<TEntity>> GetAll()
		{
			IList<TEntity> result = await Collection.Find(new BsonDocument()).ToListAsync().ConfigureAwait(false);
			return result;
		}
	}
}
