// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperationsResultV1Controller.cs" company="Microsoft">
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
    [Route(RouteTemplates.SourceControlConfigResult)]
    public class OperationsResultV1Controller : ControllerBase
    {
        private readonly ClusterConfigService clusterConfigService;

        public OperationsResultV1Controller(ClusterConfigService clusterConfigService) 
        {
            this.clusterConfigService = clusterConfigService;
        }

        /// <summary>
        /// Controller method to Get the operationsResult after deleting a resource of type Microsoft.KubernetesConfiguration/sourceControlConfiguration
        /// </summary>
        /// <param name="cancellationToken">The cancellationtoken.</param>
        /// <returns></returns>
        [HttpGet("{operationId}")]
        public async Task<ActionResult<ClusterConfigV1>> GetOperationsResult(
            [FromRoute] string operationId, 
            CancellationToken cancellationToken)
        {
            var callContext = this.Request.GetCallContext();
            if (callContext == null) 
            {
                return this.BadRequest("Invalid request URL.");
            }

            callContext.OperationsResultCall = true;

            var clusterConfigData = await clusterConfigService.GetClusterConfigurationAsync(callContext, cancellationToken).ConfigureAwait(false);
            
            var operationsResultView = new OperationsUrlV1(callContext.ConfigurationResourceName, Constants.ProvisioningStateDeleting, clusterConfigData.complianceStatus.complianceState.ToString());
            return this.Ok(operationsResultView);
        }
    }
}