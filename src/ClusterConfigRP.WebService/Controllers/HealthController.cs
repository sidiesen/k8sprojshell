//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using ClusterConfigRP.WebService.Routes;

    [ApiVersion("1.0")]
    [ApiController]
    [Route(RouteTemplates.Health)]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [Route("live")]
        public ActionResult GetLiveness()
        {
            // TODO perform a more in-depth health check. For now, just want to know that the
            // service is able to serve traffic
            return Ok();
        }

        [HttpGet]
        [Route("ready")]
        public ActionResult GetReadiness()
        {
            // This API needs to be quick and should not perform any off-box checks.
            return Ok();
        }
    }
}
