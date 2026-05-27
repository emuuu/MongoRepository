using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>	A mongoDB context for specified entity. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    public class EntityContext<TEntity>
	{
		private static readonly ConcurrentDictionary<string, MongoClient> _clientCache = new ConcurrentDictionary<string, MongoClient>();

		/// <summary>   Constructor. </summary>
		/// <param name="mongoOptions">   The mongoDB connection options. </param>
		public EntityContext(IOptions<MongoDbOptions> mongoOptions)
		{
			_entityTypeName = typeof(TEntity).Name;

			var dbAttribute = (EntityDatabaseAttribute)Attribute.GetCustomAttribute(typeof(TEntity), typeof(EntityDatabaseAttribute));
			_entityDatabaseName = dbAttribute?.Database ?? _entityTypeName;

			var collectionAttribute = (EntityCollectionAttribute)Attribute.GetCustomAttribute(typeof(TEntity), typeof(EntityCollectionAttribute));
			_entityCollectionName = collectionAttribute?.Collection ?? _entityTypeName;

			var roClient = _clientCache.GetOrAdd(mongoOptions.Value.ReadOnlyConnection, cs => new MongoClient(cs));
			_readOnlyDatabase = roClient.GetDatabase(_entityDatabaseName);

			var rwClient = _clientCache.GetOrAdd(mongoOptions.Value.ReadWriteConnection, cs => new MongoClient(cs));
			_readWriteDatabase = rwClient.GetDatabase(_entityDatabaseName);
		}


		/// <summary>   Gets the entities type name. </summary>
		/// <value> The entities type. </value>
		private readonly string _entityTypeName = null;

		/// <summary>   Gets the database the entities are stored in. </summary>
		/// <value> The entities type. </value>
		private readonly string _entityDatabaseName = null;

		/// <summary>   Gets the collection the entities are stored in. </summary>
		/// <value> The entities type. </value>
		private readonly string _entityCollectionName = null;

		/// <summary>   Gets the mongo readonly database interface. </summary>
		/// <value> The mongo readonly database interface. </value>
		private readonly IMongoDatabase _readOnlyDatabase = null;

		/// <summary>   Gets the mongo read/write database interface. </summary>
		/// <value> The mongo read/write database interface. </value>
		private readonly IMongoDatabase _readWriteDatabase = null;

		public IMongoCollection<TEntity> Collection(bool readOnly)
		{
			if(readOnly)
				return _readOnlyDatabase.GetCollection<TEntity>(_entityCollectionName);
			else
				return _readWriteDatabase.GetCollection<TEntity>(_entityCollectionName);
		}

		/// <summary>
		/// Starts a session on the read/write client. Sessions are bound to the client
		/// that backs the writable collection, so they can be passed to mutations and
		/// transactional reads on this context.
		/// </summary>
		/// <remarks>
		/// <see cref="IClientSessionHandle"/> is <see cref="IDisposable"/>; dispose it
		/// with a <c>using</c> statement to release server resources.
		/// </remarks>
		public Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default)
			=> _readWriteDatabase.Client.StartSessionAsync(cancellationToken: cancellationToken);

		/// <summary>
		/// Returns <c>true</c> when the underlying cluster supports multi-document
		/// transactions: ReplicaSet, Sharded, or LoadBalanced topology, or a
		/// direct connection to a replica set member or shard router. Returns
		/// <c>false</c> for standalone deployments and for any failure during
		/// the capability probe (connection refused, authentication denied,
		/// timeout, ...). This is a best-effort check — connection problems
		/// surface again on the next real operation, which is the right place
		/// to handle them.
		/// </summary>
		/// <remarks>
		/// Each call performs a <c>ping</c> on the <c>admin</c> database to
		/// force server discovery (the driver leaves <c>Cluster.Description.Type</c>
		/// as <c>Unknown</c> until the first operation). The result is not
		/// cached because cluster topology can change at runtime (failover,
		/// reconfig); callers that need to gate hot paths should cache the
		/// result themselves.
		/// </remarks>
		public async Task<bool> SupportsTransactionsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				// Cluster.Description.Type stays Unknown until the driver has performed
				// server discovery. A ping forces it without writing anything.
				await _readWriteDatabase.Client.GetDatabase("admin")
					.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				return false;
			}

			var description = _readWriteDatabase.Client.Cluster.Description;

			if (description.Type == ClusterType.ReplicaSet
				|| description.Type == ClusterType.Sharded
				|| description.Type == ClusterType.LoadBalanced)
				return true;

			// directConnection=true reports Cluster.Type as Standalone even when the
			// underlying server is a replica set member; check server types too.
			foreach (var server in description.Servers)
			{
				if (server.Type == ServerType.ReplicaSetPrimary
					|| server.Type == ServerType.ReplicaSetSecondary
					|| server.Type == ServerType.ShardRouter
					|| server.Type == ServerType.LoadBalanced)
					return true;
			}

			return false;
		}
	}
}
