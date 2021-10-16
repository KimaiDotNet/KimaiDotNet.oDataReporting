using MarkZither.KimaiDotNet.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class CustomerController : ControllerBase
    {
        private static IList<CustomerCollection> _customers = new List<CustomerCollection>
        {
            new CustomerCollection
            {
                Id = 1,
                Name = "Customer1",
            },
            new CustomerCollection
            {
                Id = 2,
                Name = "Customer2",
            },
            new CustomerCollection
            {
                Id = 3,
                Name = "Customer3"
            },
        };
        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(_customers);
        }
    }
}
