using Polly;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class PollyContextExtensions
    {
        private static readonly ResiliencePropertyKey<ILogger> LoggerKey = new("ILogger");

        public static ResilienceContext WithLogger<T>(this ResilienceContext context, ILogger logger)
        {
            context.Properties.Set(LoggerKey, logger);
            return context;
        }

        public static ILogger? GetLogger(this ResilienceContext context)
        {
            if (context.Properties.TryGetValue(LoggerKey, out var logger))
            {
                return logger;
            }

            return null;
        }
    }
}
