using Microsoft.AspNetCore.Mvc;
using FlowDance.Client.AspNetCore.ActionFilters;

namespace FlowDance.Tests.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [CompensationSpan(CompensatingActionUrl = "http://localhost/TripBookingService/Compensation", CompensationSpanOption = CompensationSpanOption.RequiresNew)]
        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            // Access the CompensationSpan instance from the ActionFilter
            var compensationSpan = HttpContext.Items["CompensationSpan"];

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
