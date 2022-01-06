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
    public class ActivityController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<ActivityController> _logger;
        public ActivityController(IOptions<KimaiOptions> kimaiOptions, ILogger<ActivityController> logger)
        {
            _kimaiOptions = kimaiOptions.Value ?? throw new ArgumentNullException(nameof(kimaiOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Activity";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<ActivityCollection>>(key: url));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadActivityCacheError, ex, EventIds.Cache.ReadActivityCacheError.Name);
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
            try
            {
                Barrel.Current.Add(key: url, data: activities, expireIn: TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteActivityCacheError, ex, EventIds.Cache.WriteActivityCacheError.Name);
            }

            return Ok(activities);
        }
    }
}
