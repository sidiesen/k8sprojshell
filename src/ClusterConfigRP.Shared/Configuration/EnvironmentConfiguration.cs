//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.Shared.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    using Microsoft.AspNetCore.Http;

    using ClusterConfigRP.Shared.Logging.Loggers;
    using ClusterConfigRP.Shared.Logging.Structures;

    public class EnvironmentConfiguration
    {
        public const string DpUriPrefix = "http";

        private const string dpEndpointTemplate = "http://{0}/";
        private const string dpEndpointDNS = "DPEnpointDNS";

        public static EnvironmentConfiguration Instance { get; } = new EnvironmentConfiguration();

        public ServiceEnvironment ServiceEnvironment { get; }
        public string Role { get; }
        public string Location { get; }
        public string Build { get; }
        public LogLevel CurrentLogLevel { get; }
        public bool EnableConsoleLogs { get; }
        public bool EnableFileLogs { get; }
        public bool EnableGenevaLogs { get; }
        public string StatsDHostName { get; }
        public int StatsDPort { get; }
        public string MdmNamespace { get; }
        public string ClusterConfigDPEndpoint { get; }

        public static LoggerCollection SetupLogging(IHttpContextAccessor httpContextAccessor)
        {
            GlobalLogDimensions.InitializeSingleton(
                EnvironmentConfiguration.Instance.ServiceEnvironment,
                EnvironmentConfiguration.Instance.Role,
                EnvironmentConfiguration.Instance.Location,
                EnvironmentConfiguration.Instance.Build);

            var logging = new LoggerCollection(EnvironmentConfiguration.Instance.CurrentLogLevel);
            var defaultCorrelationDetails = new LogDetails();

            var loggersToUse = new List<ILogging>();
            if (EnvironmentConfiguration.Instance.EnableGenevaLogs)
            {
                loggersToUse.Add(new GenevaLogging(
                    httpContextAccessor,
                    defaultCorrelationDetails,
                    EnvironmentConfiguration.Instance.StatsDHostName,
                    EnvironmentConfiguration.Instance.StatsDPort,
                    EnvironmentConfiguration.Instance.MdmNamespace));
            }
            if (EnvironmentConfiguration.Instance.EnableConsoleLogs)
            {
                loggersToUse.Add(new ConsoleLogging(httpContextAccessor, defaultCorrelationDetails));
            }
            if (EnvironmentConfiguration.Instance.EnableFileLogs)
            {
                loggersToUse.Add(new FileLogging(httpContextAccessor, defaultCorrelationDetails));
            }
            logging.AddOn(loggersToUse);

            return logging;
        }


        private EnvironmentConfiguration()
        {
            try
            {
                this.ServiceEnvironment = Enum.Parse<ServiceEnvironment>(Environment.GetEnvironmentVariable("Environment"));
                this.Role = System.Environment.GetEnvironmentVariable("Role");
                this.Location = System.Environment.GetEnvironmentVariable("Location");
                this.Build = System.Environment.GetEnvironmentVariable("Build");

                this.CurrentLogLevel = Enum.Parse<LogLevel>(System.Environment.GetEnvironmentVariable("LogLevel"));
                this.EnableConsoleLogs = bool.Parse(System.Environment.GetEnvironmentVariable("EnableConsoleLogs"));
                this.EnableFileLogs = bool.Parse(System.Environment.GetEnvironmentVariable("EnableFileLogs"));
                this.EnableGenevaLogs = bool.Parse(System.Environment.GetEnvironmentVariable("EnableGenevaLogs"));

                if (this.EnableGenevaLogs)
                {
                    this.EnableConsoleLogs = false; // Disable "ConsoleLogs" since Geneva Logs also write to the console. No point in duplicating.

                    this.StatsDHostName = System.Environment.GetEnvironmentVariable("StatsDHostName");
                    if (System.Environment.GetEnvironmentVariable("StatsDPort") != null)
                    {
                        this.StatsDPort = int.Parse(System.Environment.GetEnvironmentVariable("StatsDPort"));
                    }
                    this.MdmNamespace = System.Environment.GetEnvironmentVariable("MdmNamespace");
                }

                // DPEndpoint
                var dpEndpoint = System.Environment.GetEnvironmentVariable(dpEndpointDNS);

                if (string.IsNullOrEmpty(dpEndpoint))
                {
                    var error = new ClusterConfigError
                    {
                        ClusterConfigErrorCode = ClusterConfigError.ErrorCode.ReadConfigurationFailed,
                        Message = "An internal server error occurred."
                    };

                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Failed to read configuration value: {0}", dpEndpointDNS));
                    throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
                }

                // If debugging locally, the endpoint will be fully qualified
                if (dpEndpoint.StartsWith(DpUriPrefix, true, CultureInfo.InvariantCulture))
                {
                    this.ClusterConfigDPEndpoint = dpEndpoint;
                }
                else // If running inside a cluster, it will not have https.  Use the template and construct it
                {
                    this.ClusterConfigDPEndpoint = string.Format(dpEndpointTemplate, dpEndpoint);
                }
            }
            catch (Exception ex)
            {
                var error = new ClusterConfigError
                {
                    ClusterConfigErrorCode = ClusterConfigError.ErrorCode.ReadConfigurationFailed,
                    Message = "An internal server error occurred."
                };

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Exception getting configuration value: {0}; exception: {1}", dpEndpointDNS, ex.Message));
                throw new ClusterConfigException(error, HttpStatusCode.InternalServerError);
            }
        }
    }
}
