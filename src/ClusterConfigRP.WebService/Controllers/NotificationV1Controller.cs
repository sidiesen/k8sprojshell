// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using ClusterConfigRP.Models;
    using ClusterConfigRP.Service;
    using ClusterConfigRP.WebService.Configuration;
    using ClusterConfigRP.WebService.Extensions;
    using ClusterConfigRP.WebService.Routes;
    using ClusterConfigRP.WebService.ViewModels;
    using System.Net.Http;

    [ApiVersion(ApiVersions.ArmSubscriptionApiVersion)]
    [Route(RouteTemplates.ArmNotification)]
    [ApiController]
    public class NotificationV1Controller : ControllerBase
    {
        public const string ConnectedClusterExpectedActionDelete = "Microsoft.Kubernetes/connectedClusters/delete";
        public const string ManagedClusterExpectedActionDelete = "Microsoft.ContainerService/managedClusters/delete";

        private readonly ClusterConfigService clusterConfigService;

        public NotificationV1Controller(ClusterConfigService clusterConfigService)
        {
            this.clusterConfigService = clusterConfigService;
        }

        [HttpPost]
        public async Task<ActionResult> Post(
            [FromBody] ResourceProviderLinkedNotificationDefinition notification,
            CancellationToken cancellationToken)
        {
            // Check the action is Connected Clusters Delete
            if (notification == null || !notification.Action.Equals(ConnectedClusterExpectedActionDelete, StringComparison.CurrentCultureIgnoreCase))
            {
                // If the Action is not Delete, return OK and ignore the notification
                return this.Ok();
            }

            var callContext = this.Request.GetCallContext();
            if (callContext == null)
            {
                return this.BadRequest("Invalid request URL.");
            }

            // Delete all Configurations
            await this.clusterConfigService.DeleteAllConfigurationsAsync(callContext, cancellationToken).ConfigureAwait(false);

            return this.Ok();
        }
    }
}