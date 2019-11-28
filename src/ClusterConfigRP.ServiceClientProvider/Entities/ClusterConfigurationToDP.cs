//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider.Entities
{
    using Newtonsoft.Json;
    using System;

    public class ClusterConfigurationToDP
    {
        public string clusterName { get; set; }

        // DataPlane requires id, in place of ConfigurationName
        public string id { get; set; }

        public string providerName { get; set; }

        /// <summary>
        /// The Kind of the Configuration. E.g. GIT, MSFT, IT
        /// </summary>
        public string configKind { get; set; }

        /// <summary>
        /// Type of the Configuration. E.g. TINA, OMS, etc.
        /// </summary>
        public string configType { get; set; }

        public string crdNameSpace { get; set; }

        public string parameter { get; set; }
    }
}