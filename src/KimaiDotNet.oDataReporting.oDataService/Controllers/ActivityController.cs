using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.oDataReporting.oDataService.Models;
using MarkZither.KimaiDotNet.oDataReporting.oDataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;
using MarkZither.KimaiDotNet;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class ActivityController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        public ActivityController(IOptions<KimaiOptions> kimaiOptions)
        {
            _kimaiOptions = kimaiOptions.Value;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Activity";
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                return Ok(Barrel.Current.Get<List<ActivityCollection>>(key: url));
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var activities = docs.ListActivitiesUsingGet();
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            Barrel.Current.Add(key: url, data: activities, expireIn: TimeSpan.FromSeconds(secs));

            return Ok(activities);
        }
    }
}
