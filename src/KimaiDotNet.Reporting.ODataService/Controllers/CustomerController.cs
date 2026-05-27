using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Caching.Memory;
using MarkZither.KimaiDotNet;
using MarkZither.KimaiDotNet.Reporting.ODataService;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class CustomerController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<CustomerController> _logger;
        private readonly IMemoryCache _cache;
        public CustomerController(IOptions<KimaiOptions> kimaiOptions, ILogger<CustomerController> logger, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Customer";
            try
            {
                if (_cache.TryGetValue(url, out List<CustomerCollection>? cached))
                {
                    return Ok(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadCustomerCacheError, ex, EventIds.Cache.ReadCustomerCacheError.Name);
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var customers = docs.ListCustomersUsingGet();

            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try { 
            _cache.Set(url, customers, TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteCustomerCacheError, ex, EventIds.Cache.WriteCustomerCacheError.Name);
            }

            return Ok(customers);
        }
    }
}
