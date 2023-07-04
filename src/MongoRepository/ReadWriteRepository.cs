using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
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
        private static TEntity TrimStrings(TEntity entity, CancellationToken cancellationToken = default)
        {
            foreach (var property in typeof(TEntity).GetProperties())
            {
                var bsonIgnoreAttribute = (BsonIgnoreAttribute[])property.GetCustomAttributes(typeof(BsonIgnoreAttribute), false);
                if (bsonIgnoreAttribute.Length > 0)
                    continue;

                if (property.CanRead && property.CanWrite && property.PropertyType == typeof(string))
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
        public virtual ConfiguredTaskAwaitable Add(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);
            return Collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>	Adds a range asynchronously. </summary>
        /// <param name="entities">	An IEnumerable&lt;TEntity&gt; of items to append to this. </param>
        public virtual ConfiguredTaskAwaitable AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
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

            return Collection.InsertManyAsync(entities, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>	Updates the given entity asynchronously. </summary>
        /// <param name="entity">	The entity. </param>
        /// <returns>	A TEntity. </returns>
        public virtual ConfiguredTaskAwaitable<ReplaceOneResult> Update(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);

            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id);
            return Collection.ReplaceOneAsync(
                 filter,
                 entity,
                 new ReplaceOptions { IsUpsert = true }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual ConfiguredTaskAwaitable<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
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
            var updates = new List<WriteModel<TEntity>>();
            foreach (var entity in entities)
            {
                updates.Add(new ReplaceOneModel<TEntity>(Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id), entity));
            }
            return Collection
                .BulkWriteAsync(updates, new BulkWriteOptions() { IsOrdered = false }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>	Deletes the given ID asynchronously. </summary>
        /// <param name="id">	The Identifier to delete. </param>
        public virtual ConfiguredTaskAwaitable<DeleteResult> Delete(TKey id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
            return Collection.DeleteOneAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>	Deletes the given IDs. </summary>
        /// <param name="ids">	The Identifiers to delete. </param>
        public virtual ConfiguredTaskAwaitable<DeleteResult> Delete(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids);
            return Collection.DeleteManyAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filterDefinition">	A definition to filter which documents will be deleted. Defaults to an empty filter.</param>
        public virtual ConfiguredTaskAwaitable<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteManyAsync(filterDefinition ?? new BsonDocument(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>	Deletes the given ID. </summary>
        /// <param name="filter">	A linq expression to filter which documents will be deleted. </param>
        public virtual ConfiguredTaskAwaitable<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteManyAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
