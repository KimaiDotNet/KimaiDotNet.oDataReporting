using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Configuration
{
    [Serializable]
    public class GeneralChaosOptions
    {
        public bool Sentinel { get; set; }
        public Guid Id { get; set; }
        public bool AutomaticChaosInjectionEnabled { get; set; }
        public bool ClusterChaosEnabled { get; set; }
        public double ClusterChaosInjectionRate { get; set; }
        public TimeSpan Frequency { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientKey { get; set; } = string.Empty;
        public int PercentageNodesToRestart { get; set; }
        public int PercentageNodesToStop { get; set; }
        public string ResourceGroupName { get; set; } = string.Empty;
        public string VMScaleSetName { get; set; } = string.Empty;

        public ExecutionInformation ExecutionInformation { get; set; } = new();

        public List<OperationChaosOptions>? OperationChaosSettings { get; set; } = new();

        public OperationChaosOptions? GetSettingsFor(string operationKey)
            => OperationChaosSettings?.SingleOrDefault(i => i.OperationKey == operationKey);
    }
    [Serializable]
    public class ExecutionInformation
    {
        public DateTimeOffset LastTimeExecuted { get; set; }
        public DateTimeOffset ChaosStoppedAt { get; set; }
        public bool MonkeysReleased { get; set; }
    }
}
