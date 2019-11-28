//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider
{
    public class Constants
    {
        public const int JsonDeserializationMaxDepth = 10;
        public const string RequestIdHeader = "x-ms-request-id";
        public const string ClientRequestIdHeader = "x-ms-client-request-id";
        public const string ClientRequestIdDPHeader = "clientRequestId";
        public const string CorrelationRequestIdDPHeader = "correlationId";
        public const string ClientTenantIdDPHeader = "tenantId";
        public const string ForceDeleteDPHeader = "forceDelete";
        public const string OperationsResultHeader = "x-ms-operations-result";
        public const string AcceptJson = "application/json";

        public const string ApiVersionDP = "?api-version=2019-11-01-Preview";

        // ARM Provisioning State
        public const string ProvisioningStateSucceeded = "Succeeded";

        public const string ProvisioningStateDeleting = "Deleting";

        // TODO: Fix CCDP - change 'provider' to 'providers' to stay consistent with ARM Url. For now use 'provider'
        public const string DataPlaneConfigUriTemplate = "subscriptions/{0}/resourceGroups/{1}/provider/{2}/clusters/{3}/configurations/{4}" + ApiVersionDP;

        // TODO: Fix CCDP - change 'provider' to 'providers' to stay consistent with ARM Url. For now use 'provider'
        public const string DataPlaneListConfigUriTemplate = "subscriptions/{0}/resourceGroups/{1}/provider/{2}/clusters/{3}/configurations" + ApiVersionDP + "&OperatorInstanceName={4}&CrdNameSpace={5}&ConfigName={6}";

        // TODO: Fix CCDP - change 'provider' to 'providers' to stay consistent with ARM Url. For now use 'provider'
        public const string DataPlaneDeleteAllConfigsUriTemplate = "subscriptions/{0}/resourceGroups/{1}/provider/{2}/clusters/{3}/configurations/deleteAllConfigurations" + ApiVersionDP;

        public const string DataPlaneListConfigWithTokenUriTemplate = "subscriptions/{0}/resourceGroups/{1}/provider/{2}/clusters/{3}/configurations" + ApiVersionDP + "&continuationToken={4}";

        public const string ClusterResourceIdTemplate = "subscriptions/{0}/resourceGroups/{1}/providers/{2}/{3}/{4}/providers/Microsoft.KubernetesConfiguration/sourceControlConfigurations/{5}";
        public const string OperationsUriTemplate = "subscriptions/{0}/resourceGroups/{1}/providers/{2}/{3}/{4}/providers/Microsoft.KubernetesConfiguration/sourceControlConfigurations/{5}/operations/{6}/";
        public const string RPListUriTemplate = "subscriptions/{0}/resourceGroups/{1}/providers/{2}/{3}/{4}/providers/Microsoft.KubernetesConfiguration/sourceControlConfigurations?continuationToken={5}";

        #region DefaultValues
        public const string DefaultOperatorScope = "Cluster";
        public const string DefaultOperatorType = "Flux";
        public const string DefaultMessageLevel = "3";
        #endregion
    }
}