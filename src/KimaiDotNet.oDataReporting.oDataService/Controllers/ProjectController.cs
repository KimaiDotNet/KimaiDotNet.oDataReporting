using MarkZither.KimaiDotNet.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class ProjectController : ControllerBase
    {
        private static IList<ProjectCollection> _projects = new List<ProjectCollection>
        {
            new ProjectCollection
            {
                Id = 1,
                Name = "Project1",
            },
            new ProjectCollection
            {
                Id = 2,
                Name = "Project2",
            },
            new ProjectCollection
            {
                Id = 3,
                Name = "Project3",
                Customer = 1
            },            
            new ProjectCollection
            {
                Id = 4,
                Name = "Project4",
                Customer = 2
            },
        };
        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(_projects);
        }
    }
}
