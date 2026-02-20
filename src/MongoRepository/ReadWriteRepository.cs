using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository
{
    public abstract class ReadWriteRepository<TEntity, TKey> : ReadOnlyDataRepository<TEntity, TKey>,
		IReadWriteRepository<TEntity, TKey>
		where TEntity : class, IEntity<TKey>, new()
	{
        protected ReadWriteRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
		{
            var context = new EntityContext<TEntity>(mongoOptions);
            Collection = context.Collection(false);
        }

        public override IMongoCollection<TEntity> Collection { get; }


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


        public virtual Task Add(TEntity entity, InsertOneOptions options = null, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);
            return Collection.InsertOneAsync(entity, options, cancellationToken)
                .ContinueWith(_ => entity, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public virtual Task AddRange(IEnumerable<TEntity> entities, InsertManyOptions options = null, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                TrimStrings(entity);

            return Collection.InsertManyAsync(entities, options, cancellationToken: cancellationToken);
        }

        public virtual Task<ReplaceOneResult> Update(TEntity entity, ReplaceOptions replaceOptions = null, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);

            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id);
            return Collection.ReplaceOneAsync(filter, entity, replaceOptions, cancellationToken: cancellationToken);
        }

        public virtual Task<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, BulkWriteOptions bulkWriteOptions = null, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                TrimStrings(entity);

            var updates = new List<WriteModel<TEntity>>();
            foreach (var entity in entities)
            {
                updates.Add(new ReplaceOneModel<TEntity>(Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id), entity));
            }
            return Collection.BulkWriteAsync(updates, bulkWriteOptions, cancellationToken: cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(TKey id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
            return Collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids);
            return Collection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteManyAsync(filterDefinition ?? new BsonDocument(), cancellationToken: cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        }
    }
}
