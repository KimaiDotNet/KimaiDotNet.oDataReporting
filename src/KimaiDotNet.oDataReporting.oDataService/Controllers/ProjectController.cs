using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;
using MarkZither.KimaiDotNet;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class ProjectController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        public ProjectController(IOptions<KimaiOptions> kimaiOptions)
        {
            _kimaiOptions = kimaiOptions.Value;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Project";
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                return Ok(Barrel.Current.Get<List<ProjectCollection>>(key: url));
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
            Barrel.Current.Add(key: url, data: projects, expireIn: TimeSpan.FromSeconds(secs));

            return Ok(projects);
        }
    }
}
