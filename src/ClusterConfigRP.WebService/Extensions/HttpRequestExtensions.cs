// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.Extensions
{
    using System;
    using System.Web;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    using ClusterConfigRP.Models;
    using ClusterConfigRP.WebService.Common;
    using ClusterConfigRP.Shared.Logging.Structures;

    public static class HttpRequestExtensions
    {
        // Route example : "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerName}/{clusterType}/{clusterName}/providers/Microsoft.KubernetesConfiguration/sourceControlConfigurations/";
        // Based on this we will have "" at position 0, "subscriptions" at position 1, {subscriptionId} at position 2 when we split the array. Following this format for the values seen below
        public const int MinSegmentsLength = 10; // Min length route is the List API for SourceControlConfiguration

        private const int UriSubscriptionIdIndex = 2;
        private const int UriResourceGroupIndex = 4;
        private const int UriClusterProviderNameIndex = 6;
        private const int UriClusterTypeIndex = 7;
        private const int UriClusterNameIndex = 8;
        private const int UriConfigurationNameIndex = 12;

        private const string ProviderTypeConnectedClusters = "connectedclusters";
        private const string ProviderTypeManagedClusters = "managedclusters";
        private const string DataPlaneProviderNameConnectedClusters = "ConnectedClusters";
        private const string DataPlaneProviderNameManagedClusters = "ManagedClusters";

        public static CallContext GetCallContext(this HttpRequest request) 
        {
            if (request == null)
                return null;

            string absolutePath = request.Path;
            var splits = absolutePath.Split('/');

            if (!ValidPath(splits))
            {
                return null;
            }

            var subscriptionId = request.GetSubscriptionId(splits);
            var resourceGroupName = request.GetResourceGroupName(splits);
            var providerName = request.GetProviderName(splits);
            var clusterType = request.GetClusterType(splits);

            // subId, resourceGroup, providerName and clusterType are required for this to be a valid url
            if (subscriptionId == null || resourceGroupName == null || providerName == null || clusterType == null) {
                return null;
            }
            
            var tenantId = request.GetHeaderValue(Constants.ClientTenantId);
            var clientRequestId = request.GetHeaderValue(Constants.ClientRequestIdHeader);
            var correlationId = request.GetHeaderValue(Constants.CorrelationRequestIdHeader);
            var forceDeleteHeaderValue = request.GetHeaderValue(Constants.ForceDeleteHeader);

            Dimensions dimensions = new Dimensions(correlationId, tenantId, clientRequestId);

            return new CallContext()
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                ProviderName = providerName,
                ClusterType = clusterType,
                ClusterName = GetClusterName(splits),
                ConfigurationResourceName = GetConfigName(splits), 
                ClientRequestIdHeader = clientRequestId,
                CorrelationIdHeader = correlationId,
                ForceDeleteHeader = forceDeleteHeaderValue,
                Dimensions = dimensions
            };
        }

        private static bool ValidPath(string[] splits) 
        {            
            if (splits.Length < MinSegmentsLength) 
            {
                return false;
            }

            return true;
        }

        public static string GetSubscriptionId(this HttpRequest request, string[] splits)
        {
            if (splits == null || splits.Length < UriSubscriptionIdIndex + 1)
                return null;

            return splits[UriSubscriptionIdIndex];
        }

        public static string GetResourceGroupName(this HttpRequest request, string[] splits)
        {
            if (splits == null || splits.Length < UriResourceGroupIndex + 1)
                return null;

            return splits[UriResourceGroupIndex];
        }

        public static string GetProviderName(this HttpRequest request, string[] splits)
        {
            if (splits == null || splits.Length < UriClusterProviderNameIndex + 1)
                return null;

            // TODO: Validate provider name e.g. Microsoft.Kubernetes
            return splits[UriClusterProviderNameIndex];
        }

        public static string GetClusterType(this HttpRequest request, string[] splits)
        {
            if (splits == null || splits.Length < UriClusterTypeIndex + 1)
                return null;

            var clusterType = splits[UriClusterTypeIndex];

            if (clusterType.Equals(ProviderTypeConnectedClusters, StringComparison.InvariantCultureIgnoreCase))
            {
                clusterType = DataPlaneProviderNameConnectedClusters;
            }
            else if (clusterType.Equals(ProviderTypeManagedClusters, StringComparison.InvariantCultureIgnoreCase))
            {
                clusterType = DataPlaneProviderNameManagedClusters;
            }
            else
            {
                return null;
            }

            return clusterType;
        }

        public static string GetClusterName(string[] splits)
        {
            if (splits == null || splits.Length < UriClusterNameIndex + 1)
                return null;

            return splits[UriClusterNameIndex];
        }

       public static string GetConfigName(string[] splits)
        {
            if (splits == null || splits.Length < UriConfigurationNameIndex + 1)
                return null;

            // if a config name is present it is at position 12
            return splits[UriConfigurationNameIndex];
        }

        public static string GetHeaderValue(this HttpRequest request, string headerName)
        {
            if (request == null)
                return null;

            // header values can be empty or null
            var headerValue = request.Headers[headerName];
            return headerValue;
        }

    }
}