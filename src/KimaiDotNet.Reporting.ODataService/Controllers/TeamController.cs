using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;
using MarkZither.KimaiDotNet;
using MarkZither.KimaiDotNet.Reporting.ODataService;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TeamController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TeamController> _logger;

        public TeamController(IOptions<KimaiOptions> kimaiOptions, ILogger<TeamController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
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
        public IActionResult Get(CancellationToken token)
        {
            var url = "Team";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<TeamCollection>>(key: url));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadTeamCacheError, ex, EventIds.Cache.ReadTeamCacheError.Name);
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var teams = docs.ListTeamUsingGet();
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                Barrel.Current.Add(key: url, data: teams, expireIn: TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTeamCacheError, ex, EventIds.Cache.WriteTeamCacheError.Name);
            }
            return Ok(teams);
        }
    }
}
