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
    public class ProjectController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<ProjectController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        public ProjectController(IOptions<KimaiOptions> kimaiOptions, ILogger<ProjectController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
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
            var url = "Project";
            try
            {
                if (_cache.TryGetValue(url, out List<ProjectCollection>? cached))
                {
                    return Ok(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadProjectCacheError, ex, EventIds.Cache.ReadProjectCacheError.Name);
            }
            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            var client = new KimaiClient(adapter);
            var projects = await client.Api.Projects.GetAsync(cancellationToken: token) ?? [];

            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                _cache.Set(url, projects, TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteProjectCacheError, ex, EventIds.Cache.WriteProjectCacheError.Name);
            }

            return Ok(projects);
        }
    }
}
