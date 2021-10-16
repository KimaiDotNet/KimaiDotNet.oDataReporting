using MarkZither.KimaiDotNet.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class TeamController : ControllerBase
    {
        private static IList<TeamCollection> _teams = new List<TeamCollection>
        {
            new TeamCollection
            {
                Id = 1,
                Name = "Team1",
            },
            new TeamCollection
            {
                Id = 1,
                Name = "Team2",
            },
            new TeamCollection
            {
                Id = 3,
                Name = "Team3"
            },
        };
        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(_teams);
        }
    }
}
