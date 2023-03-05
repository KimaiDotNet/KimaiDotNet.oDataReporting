﻿using System;
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
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientKey { get; set; }
        public int PercentageNodesToRestart { get; set; }
        public int PercentageNodesToStop { get; set; }
        public string ResourceGroupName { get; set; }
        public string VMScaleSetName { get; set; }

        public ExecutionInformation ExecutionInformation { get; set; }

        public List<OperationChaosOptions> OperationChaosSettings { get; set; }

        public OperationChaosOptions GetSettingsFor(string operationKey) => OperationChaosSettings?.SingleOrDefault(i => i.OperationKey == operationKey);
    }
    [Serializable]
    public class ExecutionInformation
    {
        public DateTimeOffset LastTimeExecuted { get; set; }
        public DateTimeOffset ChaosStoppedAt { get; set; }
        public bool MonkeysReleased { get; set; }
    }
}
