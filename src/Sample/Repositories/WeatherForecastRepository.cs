using Microsoft.Extensions.Options;
using MongoRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Repositories
{
    public class WeatherForecastRepository : ReadWriteRepository<WeatherForecast, string>, IWeatherForecastRepository
    {
        public WeatherForecastRepository(IOptions<MongoDbOptions> mongoOptions) : base(mongoOptions)
        {

        }
    }
}
