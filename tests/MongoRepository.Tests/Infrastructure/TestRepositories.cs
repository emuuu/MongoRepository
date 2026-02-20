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
