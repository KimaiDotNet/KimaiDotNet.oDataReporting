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
using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TeamController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TeamController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public TeamController(IOptions<KimaiOptions> kimaiOptions, ILogger<TeamController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }
        private static IList<TeamCollection> _teams = new List<TeamCollection>
        {
            new TeamCollection
            {
                Id = 1,
                Name = "Team1",
            },
            new TeamCollection
            {
                Id = 2,
                Name = "Team2",
            },
            new TeamCollection
            {
                Id = 3,
                Name = "Team3"
            },
        };
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> Get(CancellationToken token)
        {
            var url = "Team";
            try
            {
                if (_cache.TryGetValue(url, out List<TeamCollection>? cached))
                {
                    return Ok(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadTeamCacheError, ex, EventIds.Cache.ReadTeamCacheError.Name);
            }
            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            HttpClientRequestAdapter innerAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            DefaultErrorMappingRequestAdapter adapter = new DefaultErrorMappingRequestAdapter(innerAdapter);
            KimaiClient client = new KimaiClient(adapter);
            var teams = await client.Api.Teams.GetAsync(cancellationToken: token) ?? [];
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                _cache.Set(url, teams, TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTeamCacheError, ex, EventIds.Cache.WriteTeamCacheError.Name);
            }
            return Ok(teams);
        }
    }
}
