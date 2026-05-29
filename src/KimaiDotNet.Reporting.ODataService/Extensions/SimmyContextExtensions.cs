using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Polly;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class SimmyContextExtensions
    {
        private static readonly ResiliencePropertyKey<GeneralChaosOptions> ChaosSettingsKey = new("ChaosSettings");

        public static ResilienceContext WithChaosSettings(this ResilienceContext context, GeneralChaosOptions options)
        {
            context.Properties.Set(ChaosSettingsKey, options);
            return context;
        }

        public static GeneralChaosOptions? GetChaosSettings(this ResilienceContext context)
        {
            context.Properties.TryGetValue(ChaosSettingsKey, out var settings);
            return settings;
        }

        public static OperationChaosOptions? GetOperationChaosSettings(this ResilienceContext context)
            => context.GetChaosSettings()?.GetSettingsFor(context.OperationKey);
    }
}
