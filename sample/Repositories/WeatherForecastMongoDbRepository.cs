using Microsoft.Extensions.Options;
using MongoRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Repositories
{
    public class WeatherForecastMongoDbRepository : ReadWriteRepository<WeatherForecast, string>, IWeatherForecastRepository
    {
        public WeatherForecastMongoDbRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
        {

        }
    }
}
