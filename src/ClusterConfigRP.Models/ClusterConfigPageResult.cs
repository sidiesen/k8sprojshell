// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Models
{
    using System;
    using System.Collections.Generic;
    
    public class ClusterConfigPageResult<T>
    {
        public IEnumerable<T> value { get; set; }

        public Uri nextLink { get; set; }
    }
}