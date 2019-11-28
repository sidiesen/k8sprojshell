//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.Service
{
    using ClusterConfigRP.Models;
    using ClusterConfigRP.ServiceClientProvider;
    using ClusterConfigRP.ServiceClientProvider.Entities;
    using ClusterConfigRP.Shared.Logging.Loggers;
    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Validation;

    using Newtonsoft.Json;

    using System;
    using System.Text;
    using System.Security.Cryptography;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using System.Net.Http;

    /// <summary>
    /// Class that implements the Service layer of Microsoft.KubernetesConfiguration RP microservice
    ///  It exposes methods that will be consumed by the WebService layer (controllers); and it uses
    ///  the ServiceClientProvider to call the APIs of KubernetesConfiguration DataPlane microservice.
    /// </summary>
    public class ClusterConfigService
    {
        public const string logListEntered = "ClusterConfigService.ListConfiguration entered. Subscription: {0}, ResourceGroup: {1}, ClusterType: {2}, ClusterName: {3}";
        public const string logCreateEntered = "ClusterConfigService.CreateConfiguration entered. Subscription: {0}, ResourceGroup: {1}, ClusterType: {2}, ClusterName: {3}, ConfigName: {4}";
        public const string logGetEntered = "ClusterConfigService.GetConfiguration entered. Subscription: {0}, ResourceGroup: {1}, ClusterType: {2}, ClusterName: {3}, ConfigName: {4}";
        public const string logDeleteEntered = "ClusterConfigService.DeleteConfiguration entered. Subscription: {0}, ResourceGroup: {1}, ClusterType: {2}, ClusterName: {3}, ConfigName: {4}";
        public const string logDeleteAllEntered = "ClusterConfigService.DeleteAllConfigurations entered. Subscription: {0}, ResourceGroup: {1}, ClusterType: {2}, ClusterName: {3}";

        private DataPlaneClientProvider dataPlaneClientProvider;
        private ILogging logging;

        public ClusterConfigService(ILogging logging)
        {
            this.logging = logging;
            this.dataPlaneClientProvider = new DataPlaneClientProvider(this.logging);
        }

        public async Task<Tuple<bool, ClusterConfigData>> CreateSourceControlConfigurationAsync(CallContext callContext, ClusterConfigData clusterConfigData, CancellationToken cancellationToken = default)
        {
            Requires.Argument("callContext", callContext).NotNull();
            Requires.Argument("clusterConfigData", clusterConfigData).NotNull();

            logging.TrackMetric("ClusterConfigService.CreateSourceControlConfigurationEntered", 1);
            logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, logCreateEntered, callContext.SubscriptionId, callContext.ResourceGroupName, callContext.ClusterType, callContext.ClusterName, callContext.ConfigurationResourceName), LogLevel.Verbose, callContext.Dimensions);

            // This call is to satisfy some validations for this config.
            string continuationToken = String.Empty;
            var response = await this.dataPlaneClientProvider.ListConfigurationsAsync(callContext, continuationToken, callContext.ConfigurationResourceName, clusterConfigData.configOperator.operatorNamespace, 
                                clusterConfigData.configOperator.operatorInstanceName, cancellationToken);

            if (response.items == null) 
            {
                logging.TrackMetric("ClusterConfigService.ListConfigurationsFailed", 1);
                logging.TrackTrace("List Configurations failed; returned null response.", LogLevel.Error, callContext.Dimensions);

                return null;
            }
            else 
            {
                var count = response.items.Count;

                // No items found with this config name or with this operatorInstanceName+operatorNamespace.
                // This is the first time this config is being created and it is valid to create this config.
                if (count == 0) 
                {
                    callContext.FirstPut = true;

                    // create new config
                    var result = await this.dataPlaneClientProvider.CreateClusterConfigurationAsync(clusterConfigData, callContext, cancellationToken);

                    return new Tuple<bool, ClusterConfigData>(
                        result.Item1,
                        ConvertToClusterConfigData(result.Item2));
                }
                // Some results were returned. So we must do further validation to understand them
                else 
                {
                    IList<ClusterConfigData> clusterConfigDataList = new List<ClusterConfigData>();
                    foreach (ClusterConfigurationFromDP clusterConfigFromDP in response.items)
                    {
                        // This is a re-PUT call so we must validate that the properties being updated are valid
                        if (callContext.ConfigurationResourceName.Equals(clusterConfigFromDP.ConfigurationName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // invalid re-put call
                            if (!CheckNoImmutablePropertiesUpdated(clusterConfigData, clusterConfigFromDP))
                            {
                                return new Tuple<bool, ClusterConfigData>(
                                    false,
                                    null);
                            } 
                            // valid re-put call
                            else
                            {
                                var result = await this.dataPlaneClientProvider.CreateClusterConfigurationAsync(clusterConfigData, callContext, cancellationToken);

                                return new Tuple<bool, ClusterConfigData>(
                                    result.Item1,
                                    ConvertToClusterConfigData(result.Item2));
                            }
                        }
                        // Validation failed because the operatorNamespace+operatorInstanceName that the user was trying to create this new config in was already
                        // used by a previous config
                        else 
                        {
                            return new Tuple<bool, ClusterConfigData>(
                                true,
                                null);
                        }
                    }
                    
                    return null;
                }
            }
        }

        public async Task<ClusterConfigData> GetClusterConfigurationAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            Requires.Argument("callContext", callContext).NotNull();

            logging.TrackMetric("ClusterConfigService.GetClusterConfigurationEntered", 1);
            logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, logGetEntered, callContext.SubscriptionId, callContext.ResourceGroupName, callContext.ClusterType, callContext.ClusterName, callContext.ConfigurationResourceName), LogLevel.Verbose, callContext.Dimensions);

            var response = await this.dataPlaneClientProvider.GetClusterConfigurationAsync(callContext, cancellationToken);

            return ConvertToClusterConfigData(response);
        }

        public async Task<ClusterConfigPageResult<ClusterConfigData>> ListConfigurationsAsync(CallContext callContext, string continuationToken, CancellationToken cancellationToken = default)
        {
            Requires.Argument("callContext", callContext).NotNull();

            logging.TrackMetric("ClusterConfigService.ListConfigurationsEntered", 1);
            logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, logListEntered, callContext.SubscriptionId, callContext.ResourceGroupName, callContext.ClusterType, callContext.ClusterName), LogLevel.Verbose, callContext.Dimensions);

            var response = await this.dataPlaneClientProvider.ListConfigurationsAsync(callContext, continuationToken, String.Empty, String.Empty, String.Empty, cancellationToken);

            if (response == null || response.items == null)
            {
                logging.TrackMetric("ClusterConfigService.ListConfigurationsFailed", 1);
                logging.TrackTrace("List Configurations failed; returned null response.", LogLevel.Error, callContext.Dimensions);

                return null;
            }

            IList<ClusterConfigData> clusterConfigDataList = new List<ClusterConfigData>();

            foreach (ClusterConfigurationFromDP clusterConfigFromDP in response.items)
            {
                clusterConfigDataList.Add(ConvertToClusterConfigData(clusterConfigFromDP));
            }

            // The currently we get the continuationToken from DP. We need to convert this into a nextLink for RP
            var nextLinkParsed = ConstructNextLink(response.continuationToken, callContext);

            var clusterConfigListResponse = new ClusterConfigPageResult<ClusterConfigData>
            {
                value = clusterConfigDataList,
                nextLink = nextLinkParsed
            };

            return clusterConfigListResponse;
        }

        public async Task<HttpResponseMessage> DeleteClusterConfigurationAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            Requires.Argument("callContext", callContext).NotNull();

            logging.TrackMetric("ClusterConfigService.DeleteClusterConfigurationEntered", 1);
            logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, logDeleteEntered, callContext.SubscriptionId, callContext.ResourceGroupName, callContext.ClusterType, callContext.ClusterName, callContext.ConfigurationResourceName), LogLevel.Verbose, callContext.Dimensions);

            // no need to check success here since the dp client provider is already doing that
            var deleted = await this.dataPlaneClientProvider.DeleteClusterConfigurationAsync(callContext, cancellationToken);

            if (callContext.ForceDeleteHeader != null && callContext.ForceDeleteHeader.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Accepted);

                // if operationId is not constructed properly this header will be empty which is acceptable.
                response.Headers.Add(Constants.OperationsResultHeader, ConstructOperationsUri(callContext));

                return response;
            }
        }

        public async Task<HttpResponseMessage> DeleteAllConfigurationsAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            Requires.Argument("callContext", callContext).NotNull();

            logging.TrackMetric("ClusterConfigService.DeleteAllConfigurationsEntered", 1);
            logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, logDeleteAllEntered, callContext.SubscriptionId, callContext.ResourceGroupName, callContext.ClusterType, callContext.ClusterName), LogLevel.Verbose, callContext.Dimensions);

            // No need to check success here since the dp client provider is already doing that and throws exception
            await this.dataPlaneClientProvider.DeleteAllConfigurationsAsync(callContext, cancellationToken);

            // Return OK, as errors are handled through exception
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        #region PrivateMethods
        public bool CheckNoImmutablePropertiesUpdated(ClusterConfigData updatedConfigData, ClusterConfigurationFromDP existingConfigData)
        {
            // Parse parameter to get other variables
            Parameter existingParameter = new Parameter();
            if (!string.IsNullOrEmpty(existingConfigData.parameter))
            {
                existingParameter = JsonConvert.DeserializeObject<Parameter>(existingConfigData.parameter);
            }

            return (updatedConfigData.configOperator.operatorNamespace.Equals(existingConfigData.OperatorNamespace, StringComparison.CurrentCultureIgnoreCase) &&
                updatedConfigData.configOperator.operatorInstanceName.Equals(existingParameter.OperatorInstanceName, StringComparison.CurrentCultureIgnoreCase) &&
                updatedConfigData.configOperator.operatorType.Equals(existingParameter.OperatorType, StringComparison.CurrentCultureIgnoreCase) &&
                updatedConfigData.configOperator.operatorScope.Equals(existingParameter.OperatorScope, StringComparison.CurrentCultureIgnoreCase));
        }
        public string GenerateMD5Hash(string configurationUri)
        {
            // generate MD5 hash based on the configuration uri
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(configurationUri);
            byte[] hash = md5.ComputeHash(bytes);
        
            // convert to operations Id string that will be used
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private string ConstructOperationsUri(CallContext callContext) 
        {
            string configUri = string.Format(CultureInfo.InvariantCulture, Constants.ClusterResourceIdTemplate, 
                                        callContext.SubscriptionId,
                                        callContext.ResourceGroupName,
                                        callContext.ProviderName,
                                        callContext.ClusterType,
                                        callContext.ClusterName,
                                        callContext.ConfigurationResourceName);

            string operationId = GenerateMD5Hash(configUri);

            string operationsResultUri = String.Empty;

            if (!String.IsNullOrEmpty(operationId)) 
            {
                operationsResultUri = string.Format(CultureInfo.InvariantCulture, Constants.OperationsUriTemplate,
                                        callContext.SubscriptionId,
                                        callContext.ResourceGroupName,
                                        callContext.ProviderName,
                                        callContext.ClusterType,
                                        callContext.ClusterName,
                                        callContext.ConfigurationResourceName,
                                        operationId);
            }

            return operationsResultUri;
        }

        private Uri ConstructNextLink(string continuationToken, CallContext callContext) 
        {
            if (string.IsNullOrEmpty(continuationToken))
            {
                return null;
            }

            // TODO: Delete this if we decide not to send the next link for the DP and stick with continuationToken
            // string continuationToken = ExtractContinuationToken(nextLink);
            
            //if (string.IsNullOrEmpty(continuationToken)) 
            //{
            //    return null;
            //}

            // encode continuationToken
            byte[] encodedByte = System.Text.ASCIIEncoding.ASCII.GetBytes(continuationToken);
            string base64EncodedToken = Convert.ToBase64String(encodedByte);

            // we only create a next link uri if there is a valid continuationToken retrieved from DP
            string resourceProviderUri = string.Format(CultureInfo.InvariantCulture, Constants.RPListUriTemplate, 
                                        callContext.SubscriptionId,
                                        callContext.ResourceGroupName,
                                        callContext.ProviderName,
                                        callContext.ClusterType,
                                        callContext.ClusterName,
                                        base64EncodedToken);

            return new Uri(resourceProviderUri, UriKind.Relative);
        }

        /// <summary>
        /// Extract the continuationToken query parameter value, if found, from the Uri passed in.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>continuationToken query parameter value.</returns>
        private string ExtractContinuationToken(string uri)
        {
            string continuationToken = string.Empty;

            // Extract string after ?
            // Split at &
            // For each string, split at =
            //   If there are exactly two parts, 
            //   And if there is a valid Key - the string to the left of =
            //   Add to the dictionary, the Key, after stripping any leading &, and Value (the string to the right of =)
            var queryParams = uri.Substring(uri.IndexOf('?') + 1).Split('&').Select(p => p.Split('=')).Where(p => p.Length == 2 & !string.IsNullOrWhiteSpace(p[0])).ToDictionary(p => p[0].Trim(new char[] { '&', '$' }), p => p[1]);

            // Do case-sensitive lookup of 'skipToken'
            if (queryParams.ContainsKey(continuationToken))
            {
                continuationToken = queryParams[continuationToken];
            }

            return continuationToken;
        }

        private ClusterConfigData ConvertToClusterConfigData(ClusterConfigurationFromDP clusterConfiguration)
        {
            Requires.Argument("clusterConfigData", clusterConfiguration).NotNull();

            ClientStatus clientStatus = new ClientStatus();
            if (!string.IsNullOrEmpty(clusterConfiguration.ClientStatus))
            {
                clientStatus = JsonConvert.DeserializeObject<ClientStatus>(clusterConfiguration.ClientStatus);
            }

            Parameter parameter = new Parameter();
            if (!string.IsNullOrEmpty(clusterConfiguration.parameter))
            {
                parameter = JsonConvert.DeserializeObject<Parameter>(clusterConfiguration.parameter);
            }

            ComplianceState complianceState = (ComplianceState)clusterConfiguration.ComplianceState;

            MessageLevel messageLevel;
            Enum.TryParse(clientStatus.MessageLevel, true, out messageLevel);

            string provisioningState = clusterConfiguration.isDeleted ?  Constants.ProvisioningStateDeleting : Constants.ProvisioningStateSucceeded;

            var clusterConfigData = new ClusterConfigData
            {
                complianceStatus = new ComplianceStatus
                {
                    complianceState = complianceState,
                    message = JsonConvert.SerializeObject(clientStatus.Message),
                    lastConfigApplied = clusterConfiguration.ClientAppliedTime,
                    messageLevel = messageLevel
                },
                configOperator = new ConfigOperator
                {
                    operatorInstanceName = parameter.OperatorInstanceName,
                    operatorNamespace = clusterConfiguration.OperatorNamespace,
                    operatorParams = parameter.OperatorParams,
                    operatorScope = parameter.OperatorScope,
                    operatorType = parameter.OperatorType
                },
                sourceControlConfiguration = new SourceControlConfiguration
                {
                    repositoryPublicKey = clientStatus.PublicKey,
                    repositoryUrl = parameter.RepositoryUrl,
                    
                },
                providerName = clusterConfiguration.ProviderName,
                configKind = clusterConfiguration.ConfigKind,
                configType = clusterConfiguration.ConfigType,
                // Always set ARM Provisioning as Succeeded
                provisioningState = provisioningState,
                configName = clusterConfiguration.ConfigurationName,
                helmOperatorEnabled = parameter.EnabledHelmOperator,
                helmOperatorProperties = parameter.HelmOperatorProperties
            };
            
            return clusterConfigData;
        }
        #endregion
    }
}
