using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.Repositories;

namespace Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IWeatherForecastRepository weatherRepository, ILogger<WeatherForecastController> logger)
        {
            _weatherRepository = weatherRepository;
            _logger = logger;
        }

        [HttpGet(Name = nameof(GetAllWeatherForecasts))]
        public async Task<IActionResult> GetAllWeatherForecasts([FromQuery] string jsonFilterDefinition = null, [FromQuery] string jsonSortingDefinition = null, [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {
            if(page.HasValue && !pageSize.HasValue)
            {
                pageSize = 10;
            }
            return Ok(await _weatherRepository.GetAll(jsonFilterDefinition: jsonFilterDefinition, jsonSortingDefinition: jsonSortingDefinition, page: page, pageSize: pageSize));
        }

        [HttpPost(Name = nameof(CreateWeatherForecast))]
        public async Task<IActionResult> CreateWeatherForecast([FromBody] WeatherForecast weatherForecast)
        {
            var result = await _weatherRepository.Add(weatherForecast);
            return CreatedAtRoute(nameof(GetSingleWeatherForecast), new { weatherForecastID = result.Id }, result);
        }

        [HttpGet("{weatherForecastID}", Name = nameof(GetSingleWeatherForecast))]
        public async Task<IActionResult> GetSingleWeatherForecast(string weatherForecastID)
        {
            return Ok(await _weatherRepository.Get(weatherForecastID));
        }

        [HttpPut("{weatherForecastID}", Name = nameof(UpdateWeatherForecast))]
        public async Task<IActionResult> UpdateWeatherForecast(string weatherForecastID, [FromBody] WeatherForecast weatherForecast)
        {
            weatherForecast.Id = weatherForecastID;
            return Ok(await _weatherRepository.Update(weatherForecast));
        }

        [HttpDelete("{weatherForecastID}", Name = nameof(DeleteWeatherForecast))]
        public async Task<IActionResult> DeleteWeatherForecast(string weatherForecastID)
        {
            await _weatherRepository.Delete(weatherForecastID);
            return Ok();
        }
    }
}
