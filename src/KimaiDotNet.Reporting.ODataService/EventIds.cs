namespace MarkZither.KimaiDotNet.Reporting.ODataService
{
    public static class EventIds{
        public static class Cache
        {
            public static readonly EventId ReadUserCacheError = new EventId(1, "Could not read User cache");
            public static readonly EventId WriteUserCacheError = new EventId(2, "Could not write User cache");
            public static readonly EventId ReadActivityCacheError = new EventId(3, "Could not read Activity cache");
            public static readonly EventId WriteActivityCacheError = new EventId(4, "Could not write Activity cache");
            public static readonly EventId ReadTimesheetCacheError = new EventId(3, "Could not read Timesheet cache");
            public static readonly EventId WriteTimesheetCacheError = new EventId(4, "Could not write Timesheet cache");
        }
    }
}
