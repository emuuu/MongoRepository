# MongoRepository
Straightforward CRUD repository for MongoDB

The main purpose of MongoRepository is to provide an as easy as possible DAL for services using MongoDB without tinkering. For each entity it will create a collection / table named after the entity.

## How to use it
Add your connection string to [appsettings.json](https://github.com/emuuu/MongoRepository/blob/master/sample/appsettings.json)
```
{
  "MongoDbOptions": {
    "ReadWriteConnection": "mongodb://your-readwrite-connectionstring",
    "ReadOnlyConnection": "mongodb://your-readonly-connectionstring"
  }
}
```
Then add an [entity](https://github.com/emuuu/MongoRepository/blob/master/sample/WeatherForecast.cs) which implements IEntity<string> which basically means it has an ID field with Bson attributes. By default the entities type name is used as name for the database and the collection as well. If you desire to use a database and/or a collection whose names differ from this you can use the referring attributes.
```
    [EntityDatabase("WeatherForecastDB")]
    [EntityCollection("WeatherForecastCollection")]
    public class WeatherForecast : IEntity<string>
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        //...
    }
```
For each entity you need a [repository](https://github.com/emuuu/MongoRepository/blob/master/sample/Repositories/IWeatherForecastRepository.cs) which [implements](https://github.com/emuuu/MongoRepository/blob/master/sample/Repositories/WeatherForecastMongoDbRepository.cs) IReadWriteRepository<TEntity, string>. It is possible to add your own methods to the repository and of course to override the default ones.
```
    public interface IWeatherForecastRepository : IReadWriteRepository<WeatherForecast, string>
    {
      Task<IList<WeatherForecast>> GetAllWeatherForecastsWith16Degree();
    }
```
```
    public class WeatherForecastMongoDbRepository : ReadWriteRepository<WeatherForecast, string>, IWeatherForecastRepository
    {
        public WeatherForecastMongoDbRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
        {

        }
        
        public async Task<IList<WeatherForecast>> GetAllWeatherForecastsWith16Degree()
        {
          var filter = Builders<WeatherForecast>.Filter.Eq(nameof(WeatherForecast.TemperatureC), 16);
          return await Collection
            .Find(filter)
            .ToListAsync();
        }
    }
```
Last thing to do is to [inject](https://github.com/emuuu/MongoRepository/blob/master/sample/Startup.cs) your connectionstring and the repository
```
  services.Configure<MongoDbOptions>(Configuration.GetSection("MongoDbOptions"));
  services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
```
