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
    public class TimesheetController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        public TimesheetController(IOptions<KimaiOptions> kimaiOptions)
        {
            _kimaiOptions = kimaiOptions.Value;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Timesheet";
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                return Ok(Barrel.Current.Get<List<TimesheetCollection>>(key: url));
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var timesheets = docs.ListTimesheetsRecordsUsingGet();
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            Barrel.Current.Add(key: url, data: timesheets, expireIn: TimeSpan.FromSeconds(secs));

            return Ok(timesheets);
        }
    }
}
