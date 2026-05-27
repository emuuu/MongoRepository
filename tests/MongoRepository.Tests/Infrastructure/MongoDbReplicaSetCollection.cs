namespace MongoRepository.Tests.Infrastructure;

[CollectionDefinition("MongoDB-RS")]
public class MongoDbReplicaSetCollection : ICollectionFixture<MongoDbReplicaSetFixture>
{
}
