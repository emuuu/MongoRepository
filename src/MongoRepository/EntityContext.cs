using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;

namespace MongoRepository
{
    /// <summary>	A mongoDB context for specified entity. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    public class EntityContext<TEntity>
	{
		/// <summary>   Constructor. </summary>
		/// <param name="mongoOptions">   The mongoDB connection options. </param>
		public EntityContext(IOptions<MongoDbOptions> mongoOptions)
		{
			_entityTypeName = typeof(TEntity).Name;

			var dbAttribute = (EntityDatabaseAttribute)Attribute.GetCustomAttribute(typeof(TEntity), typeof(EntityDatabaseAttribute));
			_entityDatabaseName = dbAttribute?.Database ?? _entityTypeName;

			var collectionAttribute = (EntityCollectionAttribute)Attribute.GetCustomAttribute(typeof(TEntity), typeof(EntityCollectionAttribute));
			_entityCollectionName = collectionAttribute?.Collection ?? _entityTypeName;

			var client = new MongoClient(mongoOptions.Value.ReadOnlyConnection);
			if (client != null)
				_readOnlyDatabase = client.GetDatabase(_entityDatabaseName);

			client = new MongoClient(mongoOptions.Value.ReadWriteConnection);
			if (client != null)
				_readWriteDatabase = client.GetDatabase(_entityDatabaseName);
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
	}
}
