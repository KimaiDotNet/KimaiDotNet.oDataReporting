using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using MarkZither.KimaiDotNet.Models;

namespace KimaiDotNet.oDataReporting.oDataService.Models
{
    public static class EdmModelBuilder
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TimesheetCollection>("Timesheet");
            builder.EntitySet<TeamCollection>("Team");
            builder.EntitySet<UserCollection>("User");

            return builder.GetEdmModel();
        }
    }
}
