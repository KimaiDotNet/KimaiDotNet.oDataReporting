using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using MarkZither.KimaiDotNet.Models;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Models
{
    public static class EdmModelBuilder
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TimesheetCollection>("Timesheet");
            builder.EntitySet<TeamCollection>("Team");
            builder.EntitySet<TeamMembership>("TeamMembership");
            builder.EntitySet<UserCollection>("User");
            builder.EntitySet<ProjectCollection>("Project");
            builder.EntitySet<ActivityCollection>("Activity");
            builder.EntitySet<CustomerCollection>("Customer");

            return builder.GetEdmModel();
        }
    }
}
