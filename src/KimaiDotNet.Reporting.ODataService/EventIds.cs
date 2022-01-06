namespace MarkZither.KimaiDotNet.Reporting.ODataService
{
    public static class EventIds{
        public static class Cache
        {
            public static readonly EventId ReadUserCacheError = new EventId(1, "Could not read User cache");
            public static readonly EventId WriteUserCacheError = new EventId(2, "Could not write User cache");
            public static readonly EventId ReadActivityCacheError = new EventId(3, "Could not read Activity cache");
            public static readonly EventId WriteActivityCacheError = new EventId(4, "Could not write Activity cache");
            public static readonly EventId ReadTimesheetCacheError = new EventId(5, "Could not read Timesheet cache");
            public static readonly EventId WriteTimesheetCacheError = new EventId(6, "Could not write Timesheet cache");
            public static readonly EventId ReadCustomerCacheError = new EventId(7, "Could not read Customer cache");
            public static readonly EventId WriteCustomerCacheError = new EventId(8, "Could not write Customer cache");
            public static readonly EventId ReadProjectCacheError = new EventId(9, "Could not read Project cache");
            public static readonly EventId WriteProjectCacheError = new EventId(10, "Could not write Project cache");
            public static readonly EventId ReadTeamCacheError = new EventId(11, "Could not read Team cache");
            public static readonly EventId WriteTeamCacheError = new EventId(12, "Could not write Team cache");
        }
    }
}
