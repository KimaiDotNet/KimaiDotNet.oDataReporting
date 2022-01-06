using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TeamMembershipController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions; 
        private readonly ILogger<TeamMembershipController> _logger;
        public TeamMembershipController(IOptions<KimaiOptions> kimaiOptions, ILogger<TeamMembershipController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "TeamMembership";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<TeamMembership>>(key: url));
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
                Barrel.Current.Add(key: url, data: teamMemberships, expireIn: TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTeamMembershipCacheError, ex, EventIds.Cache.WriteTeamMembershipCacheError.Name);
            }

            return Ok(teamMemberships);
        }
    }
}
