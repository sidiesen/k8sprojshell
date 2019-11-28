// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterConfigV1Controller.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Net.Http;

    using ClusterConfigRP.Models;
    using ClusterConfigRP.Service;
    using ClusterConfigRP.WebService.Configuration;
    using ClusterConfigRP.WebService.Routes;
    using ClusterConfigRP.WebService.ViewModels;
    using ClusterConfigRP.WebService.Filters;
    using ClusterConfigRP.WebService.Extensions;
    using ClusterConfigRP.ServiceClientProvider;

    [ApiVersion(ApiVersions.ApiV1Version)]
    [ApiController]
    [ClusterConfigExceptionFilter]
    [Route(RouteTemplates.SourceControlConfig)]
    public class ClusterConfigV1Controller : ControllerBase
    {
        private readonly ClusterConfigService clusterConfigService;

        public ClusterConfigV1Controller(ClusterConfigService clusterConfigService) 
        {
            this.clusterConfigService = clusterConfigService;
        }


        /// <summary>
        /// Controller method to Create a new resource of type Microsoft.KubernetesConfiguration/sourceControlConfiguration
        /// </summary>
        /// <param name="cancellationToken">The cancellationtoken.</param>
        /// <returns></returns>
        [HttpPut("{sourceControlConfigurationName}")]
        public async Task<ActionResult> PutConfig(
            [FromBody] ClusterConfigV1 clusterConfigV1, 
            CancellationToken cancellationToken) 
        {
            var callContext = this.Request.GetCallContext();
            if (callContext == null) 
            {
                return this.BadRequest("Invalid request URL.");
            }

            var clusterConfigData = CreateClusterConfigData(clusterConfigV1, ClusterConfigV1.ConfigKind.Git.ToString());

            var result = await clusterConfigService.CreateSourceControlConfigurationAsync(callContext, clusterConfigData, cancellationToken).ConfigureAwait(false);
           
            if (result != null && result.Item1)
            {
                if (result.Item2 == null)
                {
                    return this.BadRequest("Trying to create a new config in an existing operator namespace is not allowed");
                }

                if (callContext.FirstPut)
                {
                    return this.Created(this.Request.Path, new ClusterConfigV1(this.Request, callContext.ConfigurationResourceName, result.Item2));
                } 
                else
                {
                    return this.Ok(new ClusterConfigV1(this.Request, callContext.ConfigurationResourceName, result.Item2));
                }
            }
            else 
            {
                // Updated immutable properties
                return this.BadRequest("Updating immutable properties is not allowed.");
            }
        }

        /// <summary>
        /// Controller method to Get a resource of type Microsoft.KubernetesConfiguration/sourceControlConfiguration
        /// </summary>
        /// <param name="cancellationToken">The cancellationtoken.</param>
        /// <returns></returns>
        [HttpGet("{configName}")]
        public async Task<ActionResult<ClusterConfigV1>> GetConfig(
            CancellationToken cancellationToken)
        {
            var callContext = this.Request.GetCallContext();
            if (callContext == null) 
            {
                return this.BadRequest("Invalid request URL.");
            }
            
            var clusterConfigData = await clusterConfigService.GetClusterConfigurationAsync(callContext, cancellationToken).ConfigureAwait(false);

            var clusterConfigView = new ClusterConfigV1(this.Request, callContext.ConfigurationResourceName, clusterConfigData);
            
            return this.Ok(clusterConfigView);
        }

        /// <summary>
        /// Gets the list of configs for a cluster
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<ClusterConfigPageResult<ClusterConfigV1>>> ListClusterConfigData(
            [FromQuery] string continuationToken, 
            CancellationToken cancellationToken)
        {
            var callContext = this.Request.GetCallContext();
            if (callContext == null) 
            {
                return this.BadRequest("Invalid request URL.");
            }

            var clusterConfigDataPageResult = await clusterConfigService.ListConfigurationsAsync(callContext, continuationToken, cancellationToken).ConfigureAwait(false);

            if (clusterConfigDataPageResult == null) {
                return this.Ok(new ClusterConfigPageResult<ClusterConfigV1>{});
            }

            IList<ClusterConfigV1> clusterConfigV1List = new List<ClusterConfigV1>();

            foreach (ClusterConfigData clusterConfigData in clusterConfigDataPageResult.value) {
                clusterConfigV1List.Add(new ClusterConfigV1(this.Request, clusterConfigData.configName, clusterConfigData));
            }
            
            var clusterConfigV1ListResponse = new ClusterConfigPageResult<ClusterConfigV1>
            {
                value = clusterConfigV1List,
                nextLink = clusterConfigDataPageResult.nextLink
            };

            return this.Ok(clusterConfigV1ListResponse);
        }

        /// <summary>
        /// Controller method to Delete a resource of type Microsoft.KubernetesConfiguration/sourceControlConfiguration
        /// </summary>
        /// <param name="cancellationToken">The cancellationtoken.</param>
        /// <returns></returns>
        [HttpDelete("{configName}")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteConfig(
            CancellationToken cancellationToken)
        {
            var callContext = this.Request.GetCallContext();
            if (callContext == null) 
            {
                return this.BadRequest("Invalid request URL.");
            }
            
            var result = await this.clusterConfigService.DeleteClusterConfigurationAsync(callContext, cancellationToken).ConfigureAwait(false);
            
            if (callContext.ForceDeleteHeader != null && callContext.ForceDeleteHeader.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                return this.Ok(result);
            }
            else
            {
                return this.Accepted(result);
            }
        }

        #region PrivateMembers
        private static ClusterConfigData CreateClusterConfigData(ClusterConfigV1 clusterConfigV1, string configKind, string configType = "")
        {
            // Assign default values for empty strings
            if (String.IsNullOrEmpty(clusterConfigV1.properties.operatorNamespace.Trim())) 
            {
                clusterConfigV1.properties.operatorNamespace = "default";
            }

            if (String.IsNullOrEmpty(clusterConfigV1.properties.operatorScope.Trim()))
            {
                clusterConfigV1.properties.operatorScope = "cluster";
            }

            if (String.IsNullOrEmpty(clusterConfigV1.properties.operatorType.Trim()))
            {
                clusterConfigV1.properties.operatorType = "flux";
            }

            if (String.IsNullOrEmpty(clusterConfigV1.properties.operatorParams.Trim()) && clusterConfigV1.properties.operatorType.Equals("flux", StringComparison.InvariantCultureIgnoreCase))
            {
                clusterConfigV1.properties.operatorParams = "--git-readonly";
            }

            var clusterConfigData = new ClusterConfigData
            {
                configKind = configKind,
                configType = configType,
                sourceControlConfiguration = new SourceControlConfiguration
                {
                    repositoryUrl = clusterConfigV1.properties.repositoryUrl
                },
                configOperator = new ConfigOperator
                {
                    operatorInstanceName = clusterConfigV1.properties.operatorInstanceName,
                    operatorNamespace = clusterConfigV1.properties.operatorNamespace,
                    operatorParams = clusterConfigV1.properties.operatorParams,
                    operatorScope = clusterConfigV1.properties.operatorScope,
                    operatorType = clusterConfigV1.properties.operatorType
                },
                helmOperatorEnabled = clusterConfigV1.properties.enableHelmOperator
            };

            // Initilaize this if HELM operatorProperties if provided
            if(clusterConfigV1.properties.helmOperatorProperties != null) {
                clusterConfigData.helmOperatorProperties = new ExtensionOperatorProperties {
                    chartValues = clusterConfigV1.properties.helmOperatorProperties.chartValues,
                    chartVersion = clusterConfigV1.properties.helmOperatorProperties.chartVersion
                };
            }

            return clusterConfigData;

        }

        #endregion
    }
}