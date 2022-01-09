using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MarkZither.KimaiDotNet;
using MarkZither.KimaiDotNet.Reporting.ODataService;
using MonkeyCache.FileStore;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TimesheetController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TimesheetController> _logger;
        public TimesheetController(IOptions<KimaiOptions> kimaiOptions, ILogger<TimesheetController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Timesheet";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<TimesheetCollection>>(key: url));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadTimesheetCacheError, ex, EventIds.Cache.ReadTimesheetCacheError.Name);
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var timesheets = new List<TimesheetCollection>();
            var users = docs.ListUsersUsingGet();
            foreach (var user in users)
            {
                var usersTimesheets = docs.ListTimesheetsRecordsUsingGet(user: user.Id?.ToString(), size: "1000", orderBy: "id", order: "DESC");
                timesheets.AddRange(usersTimesheets);
            }
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                Barrel.Current.Add(key: url, data: timesheets, expireIn: TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTimesheetCacheError, ex, EventIds.Cache.WriteTimesheetCacheError.Name);
            }

            return Ok(timesheets);
        }
    }
}
