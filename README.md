# MongoRepository
Straightforward CRUD repository for MongoDB

The main purpose of MongoRepository is to provide an as easy as possible DAL for services using MongoDB without tinkering. For each entity it will create a collection / table named after the entity.

## How to use it
Add your connection string to [appsettings.json](https://github.com/emuuu/MongoRepository/blob/master/src/Sample/appsettings.json)
```
{
  "MongoDbOptions": {
    "ReadWriteConnection": "mongodb://your-readwrite-connectionstring",
    "ReadOnlyConnection": "mongodb://your-readonly-connectionstring"
  }
}
```
Then add an [entity](https://github.com/emuuu/MongoRepository/blob/master/src/Sample/WeatherForecast.cs) which implements IEntity<string> which basically means it has an ID field with Bson attributes
```
    public class WeatherForecast : IEntity<string>
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        //...
    }
```
For each entity you need a [repository](https://github.com/emuuu/MongoRepository/blob/master/src/Sample/Repositories/IWeatherForecastRepository.cs) which [implements](https://github.com/emuuu/MongoRepository/blob/master/src/Sample/Repositories/WeatherForecastRepository.cs) IReadWriteRepository<TEntity, string>. It is possible to add your own operations to the repository and of course to override the default operations.
```
    public interface IWeatherForecastRepository : IReadWriteRepository<WeatherForecast, string>
    {
      Task<WeatherForecast> GetAllWeatherForecastsWith16Degree();
    }
```
```
    public class WeatherForecastRepository : ReadWriteRepository<WeatherForecast, string>, IWeatherForecastRepository
    {
        public WeatherForecastRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
        {

        }
        
        public async Task<WeatherForecast> GetAllWeatherForecastsWith16Degree()
        {
          var filter = Builders<WeatherForecast>.Filter.Eq("temperatureC", 16);
          return await Collection
            .Find(filter)
            .ToListAsync();
        }
    }
```
Last thing to do is to [inject](https://github.com/emuuu/MongoRepository/blob/master/src/Sample/Startup.cs) your connectionstring and the repository
```
  services.Configure<MongoDbOptions>(Configuration.GetSection("MongoDbOptions"));
  services.AddTransient<IWeatherForecastRepository, WeatherForecastRepository>();
```
