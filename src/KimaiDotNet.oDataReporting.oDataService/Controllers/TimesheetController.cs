using MarkZither.KimaiDotNet.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class TimesheetController : ControllerBase
    {
        private static IList<TimesheetCollection> _timesheets = new List<TimesheetCollection>
        {
            new TimesheetCollection
            {
                Id = 1,
                User = 1,
                Description = "Zhangg",
            },
            new TimesheetCollection
            {
                Id = 2,
                User = 1,
                Description = "Jingchan",
            },
            new TimesheetCollection
            {
                Id = 3,
                User = 2,
                Description = "Hollewye"
            },
        };
        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(_timesheets);
        }
    }
}
