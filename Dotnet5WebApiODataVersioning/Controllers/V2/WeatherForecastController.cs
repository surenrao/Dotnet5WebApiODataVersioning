using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dotnet5WebApiODataVersioning.Controllers.V2
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class WeatherForecastVersionController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastVersionController> _logger;

        /// <summary>
        /// 
        /// </summary>        
        /// <param name="logger"></param>
        public WeatherForecastVersionController(ILogger<WeatherForecastVersionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(ApiVersion apiVersion, ODataQueryOptions<WeatherForecast> queryOptions)
        {
            var rng = new Random();
            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = apiVersion.ToString() + Summaries[rng.Next(Summaries.Length)]
            });

            
            var modifiedQueryUri = queryOptions.Request.QueryString.ToString();
            // add logic to modify queryOptions by manipulating modifiedQueryUri i.e. remove $top, $skip etc
            queryOptions.Request.QueryString = new QueryString(modifiedQueryUri);
            var newQueryOptions = new ODataQueryOptions<WeatherForecast>(queryOptions.Context, queryOptions.Request);

            return Ok(newQueryOptions.ApplyTo(result.AsQueryable()));
        }

        [HttpGet]
        [ResponseCache(Duration = 60)]
        [Route("cached")]
        public IEnumerable<WeatherForecast> GetCached()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}