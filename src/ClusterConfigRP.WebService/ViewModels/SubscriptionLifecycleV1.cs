//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels
{
    using System;

    public class SubscriptionLifecycleV1
    {
        public string state { get; set; }

        public DateTimeOffset registrationDate { get; set; }

        public SubscriptionLifecyclePropertiesV1 properties { get; set; }

        public class SubscriptionLifecyclePropertiesV1
        {
            public string tenantId { get; set; }

            public string locationPlacementId { get; set; }

            public string quotaId { get; set; }
        }
    }
}
