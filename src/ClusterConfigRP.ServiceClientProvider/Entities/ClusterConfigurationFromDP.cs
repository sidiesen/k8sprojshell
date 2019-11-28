//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

using ClusterConfigRP.Models;
using Newtonsoft.Json;
using System;

namespace ClusterConfigRP.ServiceClientProvider.Entities
{
    public class ClusterConfigurationFromDP
    {
        public string ClusterName { get; set; }

        // TODO: DataPlane needs to return ConfigName, instead of Id
        [JsonProperty("id")]
        public string ConfigurationName { get; set; }

        public string sourceControlUrl { get; set; }

        public DateTimeOffset? LastModifiedTime { get; set; }

        public DateTimeOffset? ClientAppliedTime { get; set; }

        public DateTimeOffset? ClientLastSeen { get; set; }

        public string ProviderName { get; set; }

        /// <summary>
        /// The Kind of the Configuration. E.g. GIT, MSFT, IT
        /// </summary>
        public string ConfigKind { get; set; }

        /// <summary>
        /// Type of the Configuration. E.g. TINA, OMS, etc.
        /// </summary>
        public string ConfigType { get; set; }

        [JsonProperty("crdNameSpace")]
        public string OperatorNamespace { get; set; }

        /// <summary>
        /// Compliance State - if the Configuration was successfully applied or not.
        /// </summary>
        public int ComplianceState { get; set; }

        public string parameter { get; set; }

        public string ClientStatus { get; set; }

        public bool isDeleted { get; set; }

        public bool isEmpty { get; set; }
    }

    /// <summary>
    /// Properties for SourceControl Configuration
    /// </summary>
    public class Parameter
    {
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// Scope can be either 'Cluster' (default) or 'Namespaced'
        /// </summary>
        public string OperatorScope { get; set; }

        /// <summary>
        /// InstanceName of the operator - given by the user
        /// </summary>
        public string OperatorInstanceName { get; set; }

        /// <summary>
        /// For GitHub, we currently support only 'Flux'
        /// </summary>
        public string OperatorType { get; set; }

        /// <summary>
        /// Parameters that will be passed directly to the Operator cmd line
        /// </summary>
        public string OperatorParams { get; set; }

        public bool EnabledHelmOperator { get; set; }

        public ExtensionOperatorProperties HelmOperatorProperties {get; set;}

        public Parameter()
        {
            this.OperatorInstanceName = "";
            this.OperatorParams = "";
            this.OperatorScope = Constants.DefaultOperatorScope;
            this.OperatorType = Constants.DefaultOperatorType;
            this.RepositoryUrl = "";
            this.EnabledHelmOperator = false;
        }
    }

    /// <summary>
    /// ClientStatus object returned from DataPlane
    /// </summary>
    public class ClientStatus
    {
        /// <summary>
        /// Message from when the operator was created (e.g. to set up the Source Control configuration)
        /// </summary>
        public Message Message { get; set; }

        /// <summary>
        /// Level of the message. Expected values are one of 1, 2 or 3, for Error, Warning or Information
        /// </summary>
        public string MessageLevel { get; set; }

        /// <summary>
        /// The Public Key generated in the Cluster and returned to the user - to be provisioned into the GitHub repo.
        /// </summary>
        public string PublicKey { get; set; }

        public ClientStatus()
        {
            // Initialize with default values, as DataPlane may return NULL for these
            this.Message = new Message();
            this.MessageLevel = Constants.DefaultMessageLevel;
            this.PublicKey = "";
        }
    }

    public class Message
    {
        public string OperatorMessage { get; set; }

        public string ClusterState { get; set; }
    }
}