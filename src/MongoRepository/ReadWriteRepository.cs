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
            Collection = _context.Collection(false);
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


        public virtual Task Add(TEntity entity, InsertOneOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);
            return session is null
                ? Collection.InsertOneAsync(entity, options, cancellationToken)
                : Collection.InsertOneAsync(session, entity, options, cancellationToken);
        }

        public virtual Task AddRange(IEnumerable<TEntity> entities, InsertManyOptions options = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                TrimStrings(entity);

            return session is null
                ? Collection.InsertManyAsync(entities, options, cancellationToken)
                : Collection.InsertManyAsync(session, entities, options, cancellationToken);
        }

        public virtual Task<ReplaceOneResult> Update(TEntity entity, ReplaceOptions replaceOptions = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            entity = TrimStrings(entity);

            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id);
            return session is null
                ? Collection.ReplaceOneAsync(filter, entity, replaceOptions, cancellationToken)
                : Collection.ReplaceOneAsync(session, filter, entity, replaceOptions, cancellationToken);
        }

        public virtual Task<BulkWriteResult<TEntity>> Update(IEnumerable<TEntity> entities, BulkWriteOptions bulkWriteOptions = null, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                TrimStrings(entity);

            var updates = new List<WriteModel<TEntity>>();
            foreach (var entity in entities)
            {
                updates.Add(new ReplaceOneModel<TEntity>(Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), entity.Id), entity));
            }
            return session is null
                ? Collection.BulkWriteAsync(updates, bulkWriteOptions, cancellationToken)
                : Collection.BulkWriteAsync(session, updates, bulkWriteOptions, cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(TKey id, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(nameof(IEntity<TKey>.Id), id);
            return session is null
                ? Collection.DeleteOneAsync(filter, cancellationToken)
                : Collection.DeleteOneAsync(session, filter, options: null, cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(IEnumerable<TKey> ids, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.In(nameof(IEntity<TKey>.Id), ids);
            return session is null
                ? Collection.DeleteManyAsync(filter, cancellationToken)
                : Collection.DeleteManyAsync(session, filter, options: null, cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(FilterDefinition<TEntity> filterDefinition, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            var filter = filterDefinition ?? new BsonDocument();
            return session is null
                ? Collection.DeleteManyAsync(filter, cancellationToken)
                : Collection.DeleteManyAsync(session, filter, options: null, cancellationToken);
        }

        public virtual Task<DeleteResult> Delete(Expression<Func<TEntity, bool>> filter, IClientSessionHandle session = null, CancellationToken cancellationToken = default)
        {
            return session is null
                ? Collection.DeleteManyAsync(filter, cancellationToken)
                : Collection.DeleteManyAsync(session, filter, options: null, cancellationToken);
        }

        /// <summary>
        /// Starts a session on the read/write client. Sessions can be passed to the
        /// session-accepting overloads of the repository's read and write methods to
        /// scope operations to a transaction.
        /// </summary>
        /// <remarks>
        /// <see cref="IClientSessionHandle"/> implements <see cref="IDisposable"/>,
        /// not <see cref="IAsyncDisposable"/>; dispose it with <c>using var session = ...</c>.
        /// </remarks>
        public virtual Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default)
            => _context.StartSessionAsync(cancellationToken);

        /// <summary>
        /// Returns <c>true</c> when the underlying cluster supports multi-document
        /// transactions (ReplicaSet or LoadBalanced topology). Returns <c>false</c>
        /// for standalone deployments. Use this to gate transactional code paths
        /// before calling <see cref="ExecuteInTransactionAsync"/>.
        /// </summary>
        public virtual Task<bool> SupportsTransactionsAsync(CancellationToken cancellationToken = default)
            => _context.SupportsTransactionsAsync(cancellationToken);

        /// <summary>
        /// Executes <paramref name="work"/> inside a Mongo transaction, committing on
        /// success and aborting on exception. Wraps the driver's
        /// <c>IClientSessionHandle.WithTransactionAsync</c>, which retries the work
        /// delegate automatically on <c>TransientTransactionError</c> and
        /// <c>UnknownTransactionCommitResult</c> errors.
        /// </summary>
        /// <remarks>
        /// The <paramref name="work"/> delegate may run more than once on transient
        /// errors — make it idempotent. Requires a cluster that supports
        /// transactions; check <see cref="SupportsTransactionsAsync"/> first when
        /// running against mixed topologies.
        /// </remarks>
        public virtual async Task ExecuteInTransactionAsync(
            Func<IClientSessionHandle, CancellationToken, Task> work,
            CancellationToken cancellationToken = default)
        {
            if (work is null) throw new ArgumentNullException(nameof(work));

            using var session = await _context.StartSessionAsync(cancellationToken).ConfigureAwait(false);
            await session.WithTransactionAsync<object>(
                async (s, ct) => { await work(s, ct).ConfigureAwait(false); return null; },
                transactionOptions: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
