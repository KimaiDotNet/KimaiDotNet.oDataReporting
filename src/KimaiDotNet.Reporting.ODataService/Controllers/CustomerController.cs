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
    public class CustomerController : ControllerBase
    {
        private readonly KimaiOptions _kimaiOptions;
        public CustomerController(IOptions<KimaiOptions> kimaiOptions)
        {
            _kimaiOptions = kimaiOptions.Value;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            var url = "Customer";
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                return Ok(Barrel.Current.Get<List<CustomerCollection>>(key: url));
            }
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var customers = docs.ListCustomersUsingGet();

            //Saves the cache and pass it a timespan for expiration
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            double secs = untilMidnight.TotalSeconds;
            Barrel.Current.Add(key: url, data: customers, expireIn: TimeSpan.FromSeconds(secs));

            return Ok(customers);
        }
    }
}
