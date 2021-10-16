using MarkZither.KimaiDotNet.Models;

namespace MarkZither.KimaiDotNet.oDataReporting.oDataService.Models
{
    internal class TeamMembership
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
    }
}