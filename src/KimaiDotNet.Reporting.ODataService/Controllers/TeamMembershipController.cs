using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Caching.Memory;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TeamMembershipController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions; 
        private readonly ILogger<TeamMembershipController> _logger;
        private readonly IMemoryCache _cache;
        public TeamMembershipController(IOptions<KimaiOptions> kimaiOptions, ILogger<TeamMembershipController> logger, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "TeamMembership";
            try
            {
                if (_cache.TryGetValue(url, out List<TeamMembership>? cached))
                {
                    return Ok(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadTeamMembershipCacheError, ex, EventIds.Cache.ReadTeamMembershipCacheError.Name);
            }

            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var teams = docs.ListTeamUsingGet();
            var teamMemberships = new List<TeamMembership>();
            int memId = 1;
            foreach (var item in teams)
            {
                var teamEntity = docs.GetTeamByIdUsingGet(item?.Id?.ToString());
                foreach (var user in teamEntity.Users)
                {
                    teamMemberships.Add(new TeamMembership() { Id = memId, TeamId = teamEntity.Id.GetValueOrDefault(), UserId = user.Id.GetValueOrDefault() });
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
