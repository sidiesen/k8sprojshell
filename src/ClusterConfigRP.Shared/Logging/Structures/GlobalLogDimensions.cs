// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Structures
{
    using System;

    using ClusterConfigRP.Shared.Configuration;

    public class GlobalLogDimensions
    {
        public ServiceEnvironment Environment { get; }
        public string Role { get; }
        public string Location { get; }
        public string Build { get; }

        private static GlobalLogDimensions instance;
        public static GlobalLogDimensions Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new InvalidOperationException("The GlobalLogDimensions singleton has not been initialized");
                }
                return instance;
            }
        }

        private GlobalLogDimensions(ServiceEnvironment environment, string role, string location, string build)
        {
            this.Environment = environment;
            this.Role = role;
            this.Location = location;
            this.Build = build;
        }

        public static void InitializeSingleton(ServiceEnvironment environment, string role, string location, string build)
        {
            instance = new GlobalLogDimensions(environment, role, location, build);
        }
    }
}
