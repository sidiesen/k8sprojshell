// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.Common
{
    public static class Constants
    {
        public const string ClientRequestIdHeader = "x-ms-client-request-id";
        public const string CorrelationRequestIdHeader = "x-ms-correlation-request-id";
        public const string ForceDeleteHeader = "x-ms-force";
        public const string ClientTenantId = "x-ms-client-tenant-id";
        public const string sourceControlConfigurationResourceType = "Microsoft.KubernetesConfiguration/sourceControlConfigurations";
        public const string VersionQueryStringKey = "api-version";

        public static class ArmModelConstants
        {
            public const int MaxResourceGroupNameLength = 80;
            public const int MaxResourceNameLength = 128;
            public const int MaxLocationLength = 128;
            public const int MaxTags = 15;
        }
    }
}