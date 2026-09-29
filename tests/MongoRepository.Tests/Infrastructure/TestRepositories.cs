using Microsoft.Extensions.Options;

namespace MongoRepository.Tests.Infrastructure;

public class TestReadOnlyRepository : ReadOnlyDataRepository<TestItem, string>
{
    public TestReadOnlyRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class TestReadWriteRepository : ReadWriteRepository<TestItem, string>
{
    public TestReadWriteRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class PlainEntityRepository : ReadWriteRepository<PlainEntity, string>
{
    public PlainEntityRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class OriginMarkerItemRepository : ReadWriteRepository<OriginMarkerItem, string>
{
    public OriginMarkerItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class ObjectIdKeyedItemRepository : ReadWriteRepository<ObjectIdKeyedItem, string>
{
    public ObjectIdKeyedItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class DriftItemRepository : ReadWriteRepository<DriftItem, string>
{
    public DriftItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class AlternateKeyItemRepository : ReadWriteRepository<AlternateKeyItem, string>
{
    public AlternateKeyItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class NoTrimItemRepository : ReadWriteRepository<NoTrimItem, string>
{
    public NoTrimItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}

public class StaticPropertyItemRepository : ReadWriteRepository<StaticPropertyItem, string>
{
    public StaticPropertyItemRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
    {
    }
}
