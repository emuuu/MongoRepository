namespace MongoRepository.Tests.Infrastructure;

// Serialization conventions and the driver's class map cache are process-wide, so this
// collection must not run alongside another one.
[CollectionDefinition("MongoDB", DisableParallelization = true)]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
}
