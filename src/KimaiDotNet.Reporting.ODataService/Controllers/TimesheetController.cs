using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MarkZither.KimaiDotNet;
using MarkZither.KimaiDotNet.Reporting.ODataService;
using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace KimaiDotNet.Reporting.ODataService.Controllers
{
    public class TimesheetController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<TimesheetController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        public TimesheetController(IOptions<KimaiOptions> kimaiOptions, ILogger<TimesheetController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> Get(CancellationToken token)
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
            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            HttpClientRequestAdapter innerAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            DefaultErrorMappingRequestAdapter adapter = new DefaultErrorMappingRequestAdapter(innerAdapter);
            KimaiClient client = new KimaiClient(adapter);
            var timesheets = new List<TimesheetCollection>();
            var users = await client.Api.Users.GetAsync(cancellationToken: token) ?? [];
            foreach (var user in users)
            {
                try
                {
                    var usersTimesheets = await client.Api.Timesheets.GetAsync(
                        requestConfiguration: q =>
                        {
                            q.QueryParameters.User = user.Id?.ToString();
                            q.QueryParameters.Size = "1000";
                            q.QueryParameters.OrderBy = "id";
                            q.QueryParameters.Order = "DESC";
                        },
                        cancellationToken: token) ?? [];

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
