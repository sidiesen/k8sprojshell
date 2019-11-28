//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.Models
{
    using ClusterConfigRP.Shared.Logging.Structures;
    
    /// <summary>
    /// Context of the call - typically the context of the Kubernetes cluster ARM resource.
    /// </summary>
    public class CallContext
    {
        private const string ProviderTypeConnectedClusters = "connectedclusters";
        private const string ProviderTypeManagedClusters = "managedclusters";
        private const string DataPlaneProviderNameConnectedClusters = "ConnectedClusters";
        private const string DataPlaneProviderNameManagedClusters = "ManagedClusters";

        /// <summary>
        /// Id of the resource in Context
        /// </summary>
        public string ArmResourceId { get; set; }

        public string Id { get; set; }

        public string TenantId { get; set; }

        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public string ProviderName { get; set; }

        public string ClusterName { get; set; }

        public string ClusterType { get; set; }

        public string ConfigurationResourceName { get; set; }

        public string ClientRequestIdHeader {get ; set; }

        public string CorrelationIdHeader {get; set; }

        public string ForceDeleteHeader {get; set; }

        public Dimensions Dimensions {get; set; }

        public bool OperationsResultCall {get; set; }

        public string ResourceInfoForLog { get; set; }
        
        public bool FirstPut {get; set; }
    }
}