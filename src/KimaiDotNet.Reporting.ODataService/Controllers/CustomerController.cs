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
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class CustomerController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<CustomerController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        public CustomerController(IOptions<KimaiOptions> kimaiOptions, ILogger<CustomerController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> Get(CancellationToken token)
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
            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            var client = new KimaiClient(adapter);
            var customers = await client.Api.Customers.GetAsync(cancellationToken: token) ?? [];

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
