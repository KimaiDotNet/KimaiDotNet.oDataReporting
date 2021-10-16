using MarkZither.KimaiDotNet.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace KimaiDotNet.oDataReporting.oDataService.Controllers
{
    public class UserController : ControllerBase
    {
        private static IList<UserCollection> _users = new List<UserCollection>
        {
            new UserCollection
            {
                Id = 1,
                Username = "User1",
            },
            new UserCollection
            {
                Id = 1,
                Username = "User2",
            },
            new UserCollection
            {
                Id = 3,
                Username = "User3"
            },
        };
        [HttpGet]
        [EnableQuery]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(_users);
        }
    }
}
