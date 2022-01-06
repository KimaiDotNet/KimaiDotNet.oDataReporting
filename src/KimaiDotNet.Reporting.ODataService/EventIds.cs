namespace MarkZither.KimaiDotNet.Reporting.ODataService
{
    public static class EventIds{
        public static readonly EventId ReadUserCacheError = new EventId(1, "Could not read User cache");
        public static readonly EventId WriteUserCacheError = new EventId(2, "Could not write User cache");
    }
}
