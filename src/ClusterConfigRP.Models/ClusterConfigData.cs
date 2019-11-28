//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.Models
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    
    /// <summary>
    /// Class that is used as broker data between the Controller and Service layer.
    /// </summary>
    public class ClusterConfigData
    {
        public string configName { get; set;}

        // Valid values are 'GIT', 'MSIT' or 'IT' only
        public string configKind { get; set; }

        public string configType { get; set; }

        public string providerName { get; set; }

        public SourceControlConfiguration sourceControlConfiguration { get; set; }

        public ConfigOperator configOperator { get; set; }

        public bool helmOperatorEnabled {get; set;}

        public ExtensionOperatorProperties helmOperatorProperties { get; set;}

        public string provisioningState { get; set; }

        public ComplianceStatus complianceStatus { get; set; }

        public ClusterConfigData()
        {
            this.sourceControlConfiguration = new SourceControlConfiguration();
            this.configOperator = new ConfigOperator();
            this.complianceStatus = new ComplianceStatus();
        }
    }

    public class ComplianceStatus
    {
        [JsonProperty("ComplianceStatus")]
        public ComplianceState complianceState { get; set; }

        [JsonProperty("clientAppliedTime")]
        public DateTimeOffset? lastConfigApplied { get; set; }

        [JsonProperty("message")]
        public string message { get; set; }

        [JsonProperty("level")]
        public MessageLevel messageLevel { get; set; }
    }

    [DefaultValue(Pending)]
    public enum ComplianceState
    {
        Unknown,
        Pending,
        Compliant,
        Noncompliant
    }

    [DefaultValue(Informational)]
    public enum MessageLevel
    {
        Error,
        Warning,
        Informational
    }

    public class ConfigOperator
    {
        public string operatorInstanceName { get; set; }

        public string operatorNamespace { get; set; }

        public string operatorType { get; set;  }

        public string operatorParams { get; set; }

        // Valid values are 'cluster' and 'namespace' only
        public string operatorScope { get; set; }
    }

    public class ExtensionOperatorProperties {

        public string chartVersion;
        
        public string chartValues;
    }
    
    public class SourceControlConfiguration
    {
        public string repositoryUrl { get; set; }

        public string repositoryPublicKey { get; set; }
    }
}
