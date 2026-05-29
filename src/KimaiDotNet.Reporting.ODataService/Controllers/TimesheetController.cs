using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MarkZither.KimaiDotNet;
using MarkZither.KimaiDotNet.Reporting.ODataService;
using Microsoft.Extensions.Caching.Memory;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TimesheetController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TimesheetController> _logger;
        private readonly IMemoryCache _cache;
        public TimesheetController(IOptions<KimaiOptions> kimaiOptions, ILogger<TimesheetController> logger, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Timesheet";
            try
            {
                if (_cache.TryGetValue(url, out List<TimesheetCollection>? cached))
                {
                    return Ok(cached);
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
                try
                {
                    var usersTimesheets = docs.ListTimesheetsRecordsUsingGet(user: user.Id?.ToString(), size: "1000", orderBy: "id", order: "DESC");

                    timesheets.AddRange(usersTimesheets);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, ex.Message, user);
                }
            }
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                _cache.Set(url, timesheets, TimeSpan.FromSeconds(secs));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteTimesheetCacheError, ex, EventIds.Cache.WriteTimesheetCacheError.Name);
            }

            return Ok(timesheets);
        }
    }
}
