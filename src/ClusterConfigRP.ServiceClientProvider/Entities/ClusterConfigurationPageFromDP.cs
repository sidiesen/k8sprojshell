//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

using System.Collections.Generic;

namespace ClusterConfigRP.ServiceClientProvider.Entities
{
    public class ClusterConfigurationPageFromDP
    {
        public ClusterConfigurationPageFromDP()
        {
            this.items = new List<ClusterConfigurationFromDP>();
            this.continuationToken = string.Empty;
        }

        public List<ClusterConfigurationFromDP> items {get; set;}

        // TODO: Change to NextLink once DP is changed. Keep consistent until then.
        public string continuationToken {get; set;}
    }
}