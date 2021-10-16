using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.oDataReporting.oDataService.Models;
using MarkZither.KimaiDotNet.oDataReporting.oDataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;

namespace MarkZither.KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class TeamMembershipController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        public TeamMembershipController(IOptions<KimaiOptions> kimaiOptions)
        {
            _kimaiOptions = kimaiOptions.Value;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "TeamMembership";
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                return Ok(Barrel.Current.Get<List<TeamMembership>>(key: url));
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
            Barrel.Current.Add(key: url, data: teamMemberships, expireIn: TimeSpan.FromSeconds(secs));
            return Ok(teamMemberships);
        }
    }
}
