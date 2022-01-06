using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    public class UserController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<UserController> _logger;
        public UserController(IOptions<KimaiOptions> kimaiOptions, ILogger<UserController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "User";
            try
            {
                //Dev handles checking if cache is expired
                if (!Barrel.Current.IsExpired(key: url))
                {
                    return Ok(Barrel.Current.Get<List<UserCollection>>(key: url));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(1, ex, "Could not read User cache");
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
                Barrel.Current.Add(key: url, data: users, expireIn: TimeSpan.FromSeconds(secs));
            } 
            catch(Exception ex)
            {
                _logger.LogError(2, ex, "Could not write User cache");
            }
            return Ok(users);
        }
    }
}
