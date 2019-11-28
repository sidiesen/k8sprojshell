// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterConfigV1.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels 
{
    using System;
    using System.ComponentModel;
    using ClusterConfigRP.Models;
    using ClusterConfigRP.WebService.Common;
    using Microsoft.AspNetCore.Http;
    using ClusterConfigRP.WebService.ModelValidators;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;
 

    public class ClusterConfigV1 : BaseArmNonTrackedViewModelV1<ClusterConfigV1.ClusterConfigPropertiesV1>
    {
        public ClusterConfigV1()
        {
            this.properties = new ClusterConfigPropertiesV1();
        }

        public ClusterConfigV1(HttpRequest request, string configurationResourceName, ClusterConfigData clusterConfigData)
        {
            if (clusterConfigData == null)
            {
                throw new ArgumentNullException(nameof(clusterConfigData));
            }

            this.name = configurationResourceName;
            this.id = BaseArmNonTrackedViewModelV1<ClusterConfigV1>.GetResourceId(request, configurationResourceName);
            this.type = Constants.sourceControlConfigurationResourceType;

            OperatorType operatorType;
            
            Enum.TryParse<OperatorType>(clusterConfigData.configOperator.operatorType, true, out operatorType);
            var lastConfigApplied = DateTime.MinValue;
            if (clusterConfigData.complianceStatus.lastConfigApplied.HasValue) {
                lastConfigApplied = clusterConfigData.complianceStatus.lastConfigApplied.Value.DateTime;
            }

            ExtensionOperatorProperties helmProperties = null;
            if (clusterConfigData.helmOperatorProperties != null) {
                helmProperties = new ExtensionOperatorProperties{
                        chartValues = clusterConfigData.helmOperatorProperties.chartValues,
                        chartVersion = clusterConfigData.helmOperatorProperties.chartVersion
                };
            }

            this.properties = new ClusterConfigPropertiesV1
            {
                operatorType = operatorType.ToString(),
                operatorInstanceName = clusterConfigData.configOperator.operatorInstanceName,
                operatorNamespace = clusterConfigData.configOperator.operatorNamespace,
                operatorScope = clusterConfigData.configOperator.operatorScope,
                operatorParams = clusterConfigData.configOperator.operatorParams,
                repositoryUrl = clusterConfigData.sourceControlConfiguration.repositoryUrl,
                repositoryPublicKey = clusterConfigData.sourceControlConfiguration.repositoryPublicKey,
                provisioningState = clusterConfigData.provisioningState,
                complianceStatus = new ComplianceStatus
                {
                    complianceState = clusterConfigData.complianceStatus.complianceState.ToString(),
                    lastConfigApplied = lastConfigApplied,
                    message = clusterConfigData.complianceStatus.message,
                    messageLevel = (MessageLevel) clusterConfigData.complianceStatus.messageLevel
                },
                enableHelmOperator = clusterConfigData.helmOperatorEnabled,
                helmOperatorProperties = helmProperties
            };
        }

        public class ClusterConfigPropertiesV1 : BaseProperties
        {
            [DefaultValue("default")]
            public string operatorNamespace {get; set; } = "default";

            public string provisioningState {get; set; }

            public ComplianceStatus complianceStatus {get; set; }

            [DefaultValue(false)]
            public bool enableHelmOperator { get; set; } = false;

            [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
            [ValidExtensionOperatorProperties("enableHelmOperator", "operatorType", "helm")]
            public ExtensionOperatorProperties helmOperatorProperties {get; set;}

            public ClusterConfigPropertiesV1()
            {
                this.complianceStatus = new ComplianceStatus();
            }

            [Required]
            public string repositoryUrl { get; set; }

            [DefaultValue("")]
            public string operatorInstanceName { get; set; } = "";

            [DefaultValue("flux")]
            [ValidOperatorType]
            public string operatorType { get; set; } = "flux";

            [DefaultValue("cluster")]
            [ValidOperatorScope]
            public string operatorScope { get; set; } = "cluster";

            [DefaultValue("--git-readonly")]
            public string operatorParams { get; set; } = "--git-readonly";

            public string repositoryPublicKey { get; set; }

            // Conditional Property Serialization - http://james.newtonking.com/json/help/index.html?topic=html/ConditionalProperties.htm
            // [JsonIgnore] prevents both serialization and deserialization
            public ConfigKind configKind { get; set; }
        }

        public enum OperatorType {
            Flux
        }

        public class ComplianceStatus {
            public string complianceState {get; set; }            

            public DateTime lastConfigApplied {get; set; }

            public string message {get; set; }

            public MessageLevel messageLevel {get; set; }
        }

        public class ExtensionOperatorProperties {
                        
            public string chartVersion { get;set; }

            public string chartValues {get;set;}
        }

        public enum MessageLevel {
            Error, 
            Warning,
            Info
        }

        public enum ConfigKind
        {
            Git,
            MSIT,
            IT
        }
    }
}