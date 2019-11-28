//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider
{
    using Microsoft.AspNetCore.Http;

    using Newtonsoft.Json;
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using ClusterConfigRP.Models;
    using ClusterConfigRP.Shared;
    using ClusterConfigRP.Shared.Configuration;
    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Logging.Loggers;
    using ClusterConfigRP.Shared.Validation;
    using ClusterConfigRP.ServiceClientProvider.Entities;

    public class DataPlaneClientProvider
    {
        private IDataPlaneHttpClient dataPlaneHttpClient;
        private HttpClientAdapter httpClientAdapter;
        private ILogging logging;

        private Uri dataPlaneBaseUri { get; set; }

        public DataPlaneClientProvider(ILogging logging)
        {
            this.logging = logging;
            this.httpClientAdapter = new HttpClientAdapter();
            this.dataPlaneHttpClient = new DataPlaneHttpClient(this.httpClientAdapter, this.logging);
            this.dataPlaneBaseUri = new Uri(EnvironmentConfiguration.Instance.ClusterConfigDPEndpoint);
        }

        public async Task<Tuple<bool, ClusterConfigurationFromDP>> CreateClusterConfigurationAsync(ClusterConfigData clusterConfigdata, CallContext callContext, CancellationToken cancellationToken = default)
        {
            try
            {
                Requires.Argument("clusterConfigdata", clusterConfigdata).NotNull();
                Requires.Argument("callContext", callContext).NotNull();

                // TODO: Hardcoding to OnPrem clusters
                callContext.ProviderName = callContext.ClusterType;

                string dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneConfigUriTemplate,
                                                    callContext.SubscriptionId,
                                                    callContext.ResourceGroupName,
                                                    callContext.ProviderName,
                                                    callContext.ClusterName,
                                                    callContext.ConfigurationResourceName);

                Uri dataPlaneCreateConfigUri = new Uri(this.dataPlaneBaseUri, dataPlaneRelativeUri);

                // Construct Request body
                SourceControlConfigParameter param = new SourceControlConfigParameter
                {
                    RepositoryUrl = clusterConfigdata.sourceControlConfiguration.repositoryUrl,
                    OperatorType = clusterConfigdata.configOperator.operatorType,
                    OperatorScope = clusterConfigdata.configOperator.operatorScope,
                    OperatorInstanceName = clusterConfigdata.configOperator.operatorInstanceName,
                    OperatorParams = clusterConfigdata.configOperator.operatorParams,
                    EnabledHelmOperator = clusterConfigdata.helmOperatorEnabled,
                    HelmOperatorProperties = clusterConfigdata.helmOperatorProperties
                };

                var clusterConfiguration = new ClusterConfigurationToDP
                {
                    clusterName = callContext.ClusterName,
                    id = callContext.ConfigurationResourceName,
                    configType = clusterConfigdata.configType,
                    configKind = clusterConfigdata.configKind.ToString(),
                    providerName = callContext.ProviderName,
                    crdNameSpace = clusterConfigdata.configOperator.operatorNamespace,
                    parameter = JsonConvert.SerializeObject(param)
                };

                var headers =
                    new Dictionary<string, string>
                        {
                           {
                                Constants.CorrelationRequestIdDPHeader,
                                callContext.CorrelationIdHeader
                            },
                            {
                                Constants.ClientRequestIdDPHeader,
                                callContext.ClientRequestIdHeader
                            },
                            {
                                Constants.ClientTenantIdDPHeader,
                                callContext.TenantId
                            },
                        };

                var response = await this.dataPlaneHttpClient.PutAsync<ClusterConfigurationFromDP, ClusterConfigurationToDP>(dataPlaneCreateConfigUri, clusterConfiguration, callContext.Dimensions, headers, cancellationToken);
                
                if (response != null)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                            {
                                // DP return HttpStatuscode.OK whether the config is newly created or updated
                                // We pass along true in our result regardless of whether it is newly created or
                                // updated and do the check in the controller

                                var result = await GetResponseContentAsync(response);

                                var output = JsonConvert.DeserializeObject<ClusterConfigurationFromDP>(result);

                                logging.TrackMetric("DataPlaneClientProvider.CreateClusterConfigurationSucceeded", 1);
                                logging.TrackTrace($"Successfully created Configuration: {callContext.ConfigurationResourceName}.", LogLevel.Verbose, callContext.Dimensions);
                                return new Tuple<bool, ClusterConfigurationFromDP>(
                                    true,
                                    output);
                            }
                        default:
                            {
                                var error = new ClusterConfigError
                                {
                                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                                    Message = response.ReasonPhrase
                                };

                                logging.TrackMetric("DataPlaneClientProvider.CreateClusterConfigurationFailed", 1);
                                logging.TrackTrace($"Unexpected Response while creating configuration: {callContext.ConfigurationResourceName}; StatusCode: {(int)response.StatusCode}, ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);
                                throw new ClusterConfigException(error, response.StatusCode);
                            }
                    }
                }
                else
                {
                    // TODO: Move error message to resource file
                    var errorMessage = "Failed to Create configuration - an unexpected error occurred.";
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.CreationFailed,
                        Message = errorMessage
                    };

                    logging.TrackMetric("DataPlaneClientProvider.CreateClusterConfigurationFailed", 1);
                    logging.TrackTrace($"Create Configuration returned Null response: {callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex) when (ex.GetType() != typeof(ClusterConfigException))
            {
                // TODO: Move error message to resource file
                var errorMessage = "Failed to Create configuration - an unexpected exception occurred.";
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                    Message = errorMessage
                };

                logging.TrackMetric("DataPlaneClientProvider.CreateClusterConfigurationException", 1);
                logging.TrackException(ex, $"Exception in create configuration:{callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ClusterConfigurationFromDP> GetClusterConfigurationAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            try
            {
                Requires.Argument("callContext", callContext).NotNull();

                // Create the Request Url and call DataPlaneHttpClient.GetWithRetryAsync
                string dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneConfigUriTemplate,
                                                    callContext.SubscriptionId,
                                                    callContext.ResourceGroupName,
                                                    callContext.ClusterType,
                                                    callContext.ClusterName,
                                                    callContext.ConfigurationResourceName);

                Uri dataPlaneGetConfigUri = new Uri(this.dataPlaneBaseUri, dataPlaneRelativeUri);

                var headers =
                    new Dictionary<string, string>
                        {
                            {
                                Constants.CorrelationRequestIdDPHeader,
                                callContext.CorrelationIdHeader
                            },
                            {
                                Constants.ClientRequestIdDPHeader,
                                callContext.ClientRequestIdHeader
                            },
                            {
                                Constants.ClientTenantIdDPHeader,
                                callContext.TenantId
                            },
                        };

                var response = await this.dataPlaneHttpClient.GetWithRetryAsync(dataPlaneGetConfigUri, callContext.Dimensions, headers, cancellationToken);

                if (response != null)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = await GetResponseContentAsync(response);

                        if (result != null)
                        {
                            logging.TrackMetric("DataPlaneClientProvider.GetClusterConfigurationSucceeded", 1);
                            logging.TrackTrace($"Successfully got Configuration: {callContext.ConfigurationResourceName}.", LogLevel.Verbose, callContext.Dimensions);
                            return JsonConvert.DeserializeObject<ClusterConfigurationFromDP>(result);
                        }
                        else
                        {
                            return new ClusterConfigurationFromDP();
                        }
                    }
                    else
                    {
                        var error = new ClusterConfigError
                        {
                            ClusterConfigErrorCode = ClusterConfigError.ErrorCode.GetFailed,
                            Message = response.ReasonPhrase
                        };

                        // We want to return a 204 no content if this call came from OperationsResultV1Controller and the cluster config was not found.
                        if (callContext.OperationsResultCall && response.StatusCode == HttpStatusCode.NotFound) 
                        {
                            logging.TrackMetric("DataPlaneClientProvider.GetClusterConfigurationNocontent", 1);
                            logging.TrackTrace($"Get Configuration failed for operations result call; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);
                            throw new ClusterConfigException(error, HttpStatusCode.NoContent);
                        }
                        else 
                        {
                            logging.TrackMetric("DataPlaneClientProvider.GetClusterConfigurationFailed", 1);
                            logging.TrackTrace($"Get Configuration failed: {callContext.ConfigurationResourceName}; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);
                            throw new ClusterConfigException(error, response.StatusCode);
                        }
                    }
                }
                else
                {
                    // TODO: Move error message to resource file
                    var errorMessage = "Failed to get configuration - an unexpected error occurred.";
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.GetFailed,
                        Message = errorMessage
                    };

                    logging.TrackMetric("DataPlaneClientProvider.GetClusterConfigurationFailed", 1);
                    logging.TrackTrace($"Get configuration returned NULL response: {callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex) when (ex.GetType() != typeof(ClusterConfigException))
            {
                // TODO: Move error message to resource file
                var errorMessage = "Failed to get configuration - an unexpected exception occurred.";
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                    Message = errorMessage
                };

                logging.TrackMetric("DataPlaneClientProvider.GetClusterConfigurationException", 1);
                logging.TrackException(ex, $"Exception in get configuration: {callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ClusterConfigurationPageFromDP> ListConfigurationsAsync(CallContext callContext, string continuationToken, string configName, string operatorNamespace, string operatorInstanceName, CancellationToken cancellationToken = default)
        {
            try
            {
                Requires.Argument<CallContext>("callContext", callContext).NotNull();

                callContext.ProviderName = callContext.ClusterType;

                // Construct Uri based on whether there is a continuationToken or not
                string dataPlaneRelativeUri = GetDataPlaneRelativeUriForList(callContext, continuationToken, operatorInstanceName, operatorNamespace, configName);

                Uri dataPlaneListConfigUri = new Uri(this.dataPlaneBaseUri, dataPlaneRelativeUri);

                var headers =
                    new Dictionary<string, string>
                        {
                              {
                                Constants.CorrelationRequestIdDPHeader,
                                callContext.CorrelationIdHeader
                            },
                            {
                                Constants.ClientRequestIdDPHeader,
                                callContext.ClientRequestIdHeader
                            },
                            {
                                Constants.ClientTenantIdDPHeader,
                                callContext.TenantId
                            },
                        };

                var response = await this.dataPlaneHttpClient.GetWithRetryAsync(dataPlaneListConfigUri, callContext.Dimensions, headers, cancellationToken);

                if (response != null)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var responseContent = await GetResponseContentAsync(response);

                        if (responseContent != null)
                        {
                            var result = JsonConvert.DeserializeObject<ClusterConfigurationPageFromDP>(responseContent);

                            logging.TrackMetric("DataPlaneClientProvider.ListConfigurationsSucceeded", 1);
                            logging.TrackTrace($"Successfully got {result.items.Count} items for Configurations list.", LogLevel.Verbose, callContext.Dimensions);

                            return result;
                        }
                        else
                        {
                            var error = new ClusterConfigError
                            {
                                ClusterConfigErrorCode = ClusterConfigError.ErrorCode.ListFailed,
                                Message = response.ReasonPhrase
                            };

                            logging.TrackMetric("DataPlaneClientProvider.ListConfigurationsFailed", 1);
                            logging.TrackTrace($"List Configurations returned NULL; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);

                            throw new ClusterConfigException(error, response.StatusCode);
                        }
                    }
                    else
                    {
                        var error = new ClusterConfigError
                        {
                            ClusterConfigErrorCode = ClusterConfigError.ErrorCode.ListFailed,
                            Message = response.ReasonPhrase
                        };

                        logging.TrackMetric("DataPlaneClientProvider.ListConfigurationsFailed", 1);
                        logging.TrackTrace($"List Configurations failed; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);

                        throw new ClusterConfigException(error, response.StatusCode);
                    }
                }
                else
                {
                    // TODO: Move error message to resource file
                    var errorMessage = "Failed to list configurations - an unexpected error occurred.";
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.ListFailed,
                        Message = errorMessage
                    };

                    logging.TrackMetric("DataPlaneClientProvider.ListConfigurationsFailed", 1);
                    logging.TrackTrace("List configurations returned NULL response.", LogLevel.Error, callContext.Dimensions);

                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex) when (ex.GetType() != typeof(ClusterConfigException))
            {
                // TODO: Move error message to resource file
                var errorMessage = "Failed to list configurations - an unexpected exception occurred.";
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                    Message = errorMessage
                };

                logging.TrackMetric("DataPlaneClientProvider.ListConfigurationsException", 1);
                logging.TrackException(ex, "Exception in list configurations.", LogLevel.Error, callContext.Dimensions);
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<bool> DeleteClusterConfigurationAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            try
            {
                Requires.Argument("callContext", callContext).NotNull();

                callContext.ProviderName = callContext.ClusterType;

                // Create the Request Url and call DataPlaneHttpClient.DeleteWithRetryAsync
                string dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneConfigUriTemplate,
                                                    callContext.SubscriptionId,
                                                    callContext.ResourceGroupName,
                                                    callContext.ProviderName,
                                                    callContext.ClusterName,
                                                    callContext.ConfigurationResourceName);

                Uri dataPlaneGetConfigUri = new Uri(this.dataPlaneBaseUri, dataPlaneRelativeUri);

                // this header can be null and dp handles that fine
                if (callContext.ForceDeleteHeader != null && callContext.ForceDeleteHeader.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                {
                    logging.TrackMetric("DataPlaneClientProvider.DeleteClusterConfigurationForceDelete", 1);
                    logging.TrackTrace($"Force deleting configuration: {callContext.ConfigurationResourceName}.", LogLevel.Verbose, callContext.Dimensions);
                }

                var headers =
                    new Dictionary<string, string>
                        {
                            {
                                Constants.CorrelationRequestIdDPHeader,
                                callContext.CorrelationIdHeader
                            },
                            {
                                Constants.ClientRequestIdDPHeader,
                                callContext.ClientRequestIdHeader
                            },
                            {
                                Constants.ClientTenantIdDPHeader,
                                callContext.TenantId
                            },
                            {    Constants.ForceDeleteDPHeader,
                                callContext.ForceDeleteHeader
                            }
                        };

                var response = await this.dataPlaneHttpClient.DeleteWithRetriesAsync(dataPlaneGetConfigUri, callContext.Dimensions, headers, cancellationToken);

                if (response != null)
                {
                    // Deleted successfully
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        logging.TrackMetric("DataPlaneClientProvider.DeleteClusterConfigurationSucceeded", 1);
                        logging.TrackTrace($"Successfully deleted configuration: {callContext.ConfigurationResourceName}; StatusCode: {(int)response.StatusCode}.", LogLevel.Verbose, callContext.Dimensions);
                        return (int) response.StatusCode == StatusCodes.Status200OK;
                    }
                    else
                    {
                        var error = new ClusterConfigError
                        {
                            ClusterConfigErrorCode = ClusterConfigError.ErrorCode.DeleteConfigurationFailed,
                            Message = response.ReasonPhrase
                        };

                        logging.TrackMetric("DataPlaneClientProvider.DeleteClusterConfigurationFailed", 1);
                        logging.TrackTrace($"Delete configuration failed: {callContext.ConfigurationResourceName}; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);
                        throw new ClusterConfigException(error, response.StatusCode);
                    }
                }
                else
                {
                    // TODO: Move error message to resource file
                    var errorMessage = "Failed to delete configuration - an unexpected error occurred.";
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.DeleteConfigurationFailed,
                        Message = errorMessage
                    };

                    logging.TrackMetric("DataPlaneClientProvider.DeleteClusterConfigurationFailed", 1);
                    logging.TrackTrace($"Delete configuration returned NULL response: {callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex) when (ex.GetType() != typeof(ClusterConfigException))
            {
                // TODO: Move error message to resource file
                var errorMessage = "Failed to delete configuration - an unexpected exception occurred.";
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                    Message = errorMessage
                };

                logging.TrackMetric("DataPlaneClientProvider.DeleteClusterConfigurationException", 1);
                logging.TrackException(ex, $"Exception in delete configuration: {callContext.ConfigurationResourceName}.", LogLevel.Error, callContext.Dimensions);
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }

        public async Task DeleteAllConfigurationsAsync(CallContext callContext, CancellationToken cancellationToken = default)
        {
            try
            {
                Requires.Argument("callContext", callContext).NotNull();

                callContext.ProviderName = callContext.ClusterType;

                // Create the Request Url and call DataPlaneHttpClient.PostAsync
                string dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneDeleteAllConfigsUriTemplate,
                                                    callContext.SubscriptionId,
                                                    callContext.ResourceGroupName,
                                                    callContext.ProviderName,
                                                    callContext.ClusterName);

                Uri dataPlaneGetConfigUri = new Uri(this.dataPlaneBaseUri, dataPlaneRelativeUri);

                var headers =
                    new Dictionary<string, string>
                        {
                            {
                                Constants.CorrelationRequestIdDPHeader,
                                callContext.CorrelationIdHeader
                            },
                            {
                                Constants.ClientRequestIdDPHeader,
                                callContext.ClientRequestIdHeader
                            },
                            {
                                Constants.ClientTenantIdDPHeader,
                                callContext.TenantId
                            }
                        };

                // DeleteAllConfigurations doesn't take any content, so set content to empty string.
                var content = "";

                var response = await this.dataPlaneHttpClient.PostAsync(dataPlaneGetConfigUri, content, callContext.Dimensions, headers, cancellationToken);

                if (response != null)
                {
                    // If deleted or if cluster not found, return OK
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        logging.TrackMetric("DataPlaneClientProvider.DeleteAllConfigurationsSucceeded", 1);
                        logging.TrackTrace($"Successfully deleted all configurations: {callContext.ClusterName}; StatusCode: {(int)response.StatusCode}.", LogLevel.Verbose, callContext.Dimensions);
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        logging.TrackMetric("DataPlaneClientProvider.DeleteAllConfigurationsNotFound", 1);
                        logging.TrackTrace($"Got NotFound for delete all configurations: {callContext.ClusterName}; StatusCode: {(int)response.StatusCode}.", LogLevel.Verbose, callContext.Dimensions);
                    }
                    else
                    {
                        var error = new ClusterConfigError
                        {
                            ClusterConfigErrorCode = ClusterConfigError.ErrorCode.DeleteAllConfigurationsFailed,
                            Message = response.ReasonPhrase
                        };

                        logging.TrackMetric("DataPlaneClientProvider.DeleteAllConfigurationsFailed", 1);
                        logging.TrackTrace($"Delete all configurations failed: {callContext.ClusterName}; StatusCode: {(int)response.StatusCode}; ReasonPhrase: {response.ReasonPhrase}.", LogLevel.Error, callContext.Dimensions);
                        throw new ClusterConfigException(error, response.StatusCode);
                    }
                }
                else
                {
                    // TODO: Move error message to resource file
                    var errorMessage = "Failed to delete configurations - an unexpected error occurred.";
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.DeleteAllConfigurationsFailed,
                        Message = errorMessage
                    };

                    logging.TrackMetric("DataPlaneClientProvider.DeleteAllConfigurationsFailed", 1);
                    logging.TrackTrace($"Delete all configurations returned NULL response: {callContext.ClusterName}.", LogLevel.Error, callContext.Dimensions);
                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex) when (ex.GetType() != typeof(ClusterConfigException))
            {
                // TODO: Move error message to resource file
                var errorMessage = "Failed to delete all configurations - an unexpected exception occurred.";
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.UnexpectedResponse,
                    Message = errorMessage
                };

                logging.TrackMetric("DataPlaneClientProvider.DeleteAllConfigurationsException", 1);
                logging.TrackException(ex, $"Exception in delete all configurations: {callContext.ClusterName}.", LogLevel.Error, callContext.Dimensions);
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }

        #region PrivateMethods
        private static async Task<string> GetResponseContentAsync(HttpResponseMessage response)
        {
            Requires.Argument("response", response).NotNull();

            return await response?.Content.ReadAsStringAsync();
        }

        private string GetDataPlaneRelativeUriForList(CallContext callContext, string continuationToken, string operatorInstanceName, string operatorNamespace, string configName)
        {
            Requires.Argument<CallContext>("callContext", callContext).NotNull();

            string dataPlaneRelativeUri;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                string decodedContinuationToken;
                try
                {
                    // decode continuationToken
                    byte[] decodedByte = Convert.FromBase64String(continuationToken);
                    decodedContinuationToken = Encoding.ASCII.GetString(decodedByte);

                    dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneListConfigWithTokenUriTemplate,
                                            callContext.SubscriptionId,
                                            callContext.ResourceGroupName,
                                            callContext.ProviderName,
                                            callContext.ClusterName,
                                            decodedContinuationToken);
                }
                catch (FormatException ex)
                {
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.InvalidContinuationTokenFormat,
                        Message = ex.Message
                    };

                    logging.TrackException(ex, "Exception in creating DataPlane Uri.", LogLevel.Error);
                    throw new ClusterConfigException(error, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                dataPlaneRelativeUri = string.Format(CultureInfo.InvariantCulture, Constants.DataPlaneListConfigUriTemplate,
                                        callContext.SubscriptionId,
                                        callContext.ResourceGroupName,
                                        callContext.ProviderName,
                                        callContext.ClusterName,
                                        operatorInstanceName,
                                        operatorNamespace,
                                        configName);
            }

            return dataPlaneRelativeUri;
        }
        #endregion
    }
}
