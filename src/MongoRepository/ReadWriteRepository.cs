using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>	A mongo read/write repository. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    /// <typeparam name="TKey">   	Type of the key. </typeparam>
    public abstract class ReadWriteRepository<TEntity, TKey> : ReadOnlyDataRepository<TEntity, TKey>,
		IReadWriteRepository<TEntity, TKey>
		where TEntity : class, IEntity<TKey>, new()
	{
        /// <summary>   Constructor. </summary>
        /// <param name="mongoOptions">   The mongoDB connection options. </param>
        protected ReadWriteRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
		{
            var context = new EntityContext<TEntity>(mongoOptions);
            Collection = context.Collection(false);
        }

        /// <summary>   Gets the mongoDB collection. </summary>
        /// <value> The mongoDB collection. </value>
        public override IMongoCollection<TEntity> Collection { get; }


        /// <summary>	Avoids leading or trailing whitespaces in string values. </summary>
        /// <param name="entity">	The entity to trim. </param>
        /// <returns>	A TEntity. </returns>
        private static TEntity TrimStrings(TEntity entity)
        {
            foreach (var property in typeof(TEntity).GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = (string)property.GetValue(entity);
                    if (!string.IsNullOrWhiteSpace(value))
                        property.SetValue(entity, value.Trim());
                }
            }
            return entity;
        }

        /// <summary>	Adds entity asynchronously. </summary>
        /// <param name="entity">	The entity to add. </param>
        /// <returns>	A TEntity. </returns>
        public virtual async Task<TEntity> Add(TEntity entity)
        {
            entity = TrimStrings(entity);
            await Collection.InsertOneAsync(entity).ConfigureAwait(false);
            return entity;
        }

        /// <summary>	Adds a range asynchronously. </summary>
        /// <param name="entities">	An IEnumerable&lt;TEntity&gt; of items to append to this. </param>
        public virtual async Task AddRange(IEnumerable<TEntity> entities)
        {
            foreach (var property in typeof(TEntity).GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    foreach (var entity in entities)
                    {
                        var value = (string)property.GetValue(entity);
                        if (!string.IsNullOrWhiteSpace(value))
                            property.SetValue(entity, value.Trim());
                    }
                }
            }

            await Collection.InsertManyAsync(entities).ConfigureAwait(false);
        }

        /// <summary>	Updates the given entity asynchronously. </summary>
        /// <param name="entity">	The entity. </param>
        /// <returns>	A TEntity. </returns>
        public virtual async Task<TEntity> Update(TEntity entity)
        {
            entity = TrimStrings(entity);

            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id);
            await Collection.ReplaceOneAsync(
                 filter,
                 entity,
                 new ReplaceOptions { IsUpsert = true })
                .ConfigureAwait(false);
            
            return entity;
        }

        /// <summary>	Deletes the given ID asynchronously. </summary>
        /// <param name="id">	The Identifier to delete. </param>
        public virtual async Task Delete(TKey id)
        {
            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
            await Collection.DeleteOneAsync(filter).ConfigureAwait(false);
        }

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filterDefinition">	A definition to filter which documents will be deleted. Defaults to an empty filter.</param>
        public virtual async Task Delete(FilterDefinition<TEntity> filterDefinition)
        {
            await Collection.DeleteManyAsync(filterDefinition ?? new BsonDocument()).ConfigureAwait(false);
        }

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filter">	A linq expression to filter which documents will be deleted. </param>
        public virtual async Task Delete(Expression<Func<TEntity, bool>> filter)
        {
            await Collection.DeleteManyAsync(filter).ConfigureAwait(false);
        }
    }
}
