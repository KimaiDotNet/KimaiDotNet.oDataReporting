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
    public class ProjectController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<ProjectController> _logger;
        public ProjectController(IOptions<KimaiOptions> kimaiOptions, ILogger<ProjectController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Project";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<ProjectCollection>>(key: url));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadProjectCacheError, ex, EventIds.Cache.ReadProjectCacheError.Name);
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var projects = docs.ListProjectUsingGet();

            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                Barrel.Current.Add(key: url, data: projects, expireIn: TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteProjectCacheError, ex, EventIds.Cache.WriteProjectCacheError.Name);
            }

            return Ok(projects);
        }
    }
}
