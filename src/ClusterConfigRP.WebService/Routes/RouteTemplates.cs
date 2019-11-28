// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.Routes
{
    public static class RouteTemplates
    {
        public const string ClusterExtensionPrefix = "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerName}/{clusterType}/{clusterName}/providers/Microsoft.KubernetesConfiguration";
        public const string SourceControlConfig = ClusterExtensionPrefix + "/sourceControlConfigurations/";
        public const string SourceControlConfigResult = "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerName}/{clusterType}/{clusterName}/providers/Microsoft.KubernetesConfiguration/sourceControlConfigurations/{configName}/operations/";
        public const string SubscriptionsRoute = "/subscriptions/{subscriptionId}";
        public const string Health = "/health";
        public const string ArmNotification = ClusterExtensionPrefix + "/notify";
    }
}
