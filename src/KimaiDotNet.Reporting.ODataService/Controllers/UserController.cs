using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Caching.Memory;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    public class UserController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<UserController> _logger;
        private readonly IMemoryCache _cache;
        public UserController(IOptions<KimaiOptions> kimaiOptions, ILogger<UserController> logger, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "User";
            try
            {
                if (_cache.TryGetValue(url, out List<UserCollection>? cached))
                {
                    return Ok(cached);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(EventIds.Cache.ReadUserCacheError, ex, EventIds.Cache.ReadUserCacheError.Name);
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var users = docs.ListUsersUsingGet();
            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            try
            {
                _cache.Set(url, users, TimeSpan.FromSeconds(secs));
            } 
            catch(Exception ex)
            {
                _logger.LogError(EventIds.Cache.WriteUserCacheError, ex, EventIds.Cache.WriteUserCacheError.Name);
            }
            return Ok(users);
        }
    }
}
