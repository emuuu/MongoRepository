using MongoRepository;

namespace Sample.Repositories
{
    public interface IWeatherForecastRepository : IReadWriteRepository<WeatherForecast, string>
    {
    }
}
