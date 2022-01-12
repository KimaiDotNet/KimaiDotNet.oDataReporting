using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Polly;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class SimmyContextExtensions
    {
        public const string ChaosSettings = "ChaosSettings";
        public static Context WithChaosSettings(this Context context, GeneralChaosOptions options)
        {
            context[ChaosSettings] = options;
            return context;
        }

        public static GeneralChaosOptions GetChaosSettings(this Context context) => context.GetSetting<GeneralChaosOptions>(ChaosSettings);
        public static OperationChaosOptions GetOperationChaosSettings(this Context context) => context.GetChaosSettings()?.GetSettingsFor(context.OperationKey);
        private static T GetSetting<T>(this Context context, string key)
        {
            if (context.TryGetValue(key, out object setting))
            {
                if (setting is T)
                {
                    return (T)setting;
                }
            }
            return default;
        }
    }
}
