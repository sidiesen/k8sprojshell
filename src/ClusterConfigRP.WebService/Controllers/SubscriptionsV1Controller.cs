//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.Controllers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using ClusterConfigRP.WebService.Configuration;
    using ClusterConfigRP.WebService.Routes;
    using ClusterConfigRP.WebService.ViewModels;

    [ApiVersion(ApiVersions.ArmSubscriptionApiVersion)]
    [ApiController]
    [Route(RouteTemplates.SubscriptionsRoute)]
    public class SubscriptionsV1Controller : ControllerBase
    {
        public async Task<IActionResult> Put([FromRoute] Guid subscriptionId, [FromBody] SubscriptionLifecycleV1 subscriptionLifecycle, CancellationToken cancellationToken)
        {
            // TODO - Log the request
            // Log Informational (string.Format("Subscription API called for SubId {0}, State {1}", subscriptionId, string.IsNullOrWhiteSpace(subscriptionLifecycle.state) ? string.Empty : subscriptionLifecycle.state));

            return this.Ok();
        }
    }
}
