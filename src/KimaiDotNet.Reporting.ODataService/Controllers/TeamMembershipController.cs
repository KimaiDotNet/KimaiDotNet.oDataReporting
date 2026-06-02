using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using ODataTeamMembership = MarkZither.KimaiDotNet.Reporting.ODataService.Models.TeamMembership;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Caching.Memory;
using MarkZither.KimaiDotNet;
using KimaiDotNet.Reporting.ODataService;
using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TeamMembershipController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TeamMembershipController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        public TeamMembershipController(IOptions<KimaiOptions> kimaiOptions, ILogger<TeamMembershipController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
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
            var url = "TeamMembership";
            try
            {
                if (_cache.TryGetValue(url, out List<ODataTeamMembership>? cached))
                {
                    return Ok(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadTeamMembershipCacheError, ex, EventIds.Cache.ReadTeamMembershipCacheError.Name);
            }

            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            HttpClientRequestAdapter innerAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            DefaultErrorMappingRequestAdapter adapter = new DefaultErrorMappingRequestAdapter(innerAdapter);
            KimaiClient client = new KimaiClient(adapter);
            var teams = await client.Api.Teams.GetAsync(cancellationToken: token) ?? [];
            var teamMemberships = new List<ODataTeamMembership>();
            int memId = 1;
            foreach (var item in teams)
            {
                var teamEntity = await client.Api.Teams[item?.Id?.ToString() ?? "0"].GetAsync(cancellationToken: token);
                if (teamEntity == null) continue;
                foreach (var member in teamEntity.Members ?? [])
                {
                    teamMemberships.Add(new ODataTeamMembership() { Id = memId, TeamId = teamEntity.Id.GetValueOrDefault(), UserId = member.User?.Id.GetValueOrDefault() ?? 0 });
                    memId++;
                }
            }
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                _cache.Set(url, teamMemberships, TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTeamMembershipCacheError, ex, EventIds.Cache.WriteTeamMembershipCacheError.Name);
            }

            return Ok(teamMemberships);
        }
    }
}
