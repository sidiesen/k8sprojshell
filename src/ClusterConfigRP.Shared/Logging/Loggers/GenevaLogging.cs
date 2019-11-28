// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Loggers
{
    using System;
    using Microsoft.AspNetCore.Http;
    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Logging.Utilities;

    using StatsN;
    /// <summary>
    /// Logs to Geneva making use of the MDM and FluentD containers running in the cluster.
    ///     Uses StatsD to emit metrics to MDM, which is configured to listen on a specific port.
    ///     Writes everything else to the console, which FluentD is configured to pick up.
    /// </summary>
    public class GenevaLogging : ILogging
    {
        private readonly IHttpContextAccessor contextAccessor;
        private LogDetails defaultLogDetails;
        private readonly string mdmNamespace;
        private readonly Statsd statsd;

        public GenevaLogging(
            IHttpContextAccessor contextAccessor,
            LogDetails defaultLogDetails,
            string host,
            int port,
            string mdmNamespace)
            : this(defaultLogDetails, host, port, mdmNamespace)
        {
            this.contextAccessor = contextAccessor;
        }

        public GenevaLogging(
            LogDetails defaultLogDetails,
            string host,
            int port,
            string mdmNamespace)
        {
            this.defaultLogDetails = defaultLogDetails;
            this.mdmNamespace = mdmNamespace;
            statsd = Statsd.New(new StatsdOptions { HostOrIp = host, Port = port });
        }

        public void TrackEvent(Enum eventName, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateEventJson(eventName, level, additionalDimensions);
            Console.WriteLine(log);
        }

        public void TrackException(Exception e, string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateExceptionJson(e, message, level, additionalDimensions);
            Console.WriteLine(log);
        }

        public void TrackMetric(string name, long value, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string metricDimensions = logDetails.GenerateMetricDimensions(additionalDimensions);
            statsd.GaugeAsync($"{{\"Namespace\":\"{mdmNamespace}\",\"Metric\":\"{name}\", \"Dims\":{metricDimensions}}}", value);
        }

        public void TrackTrace(string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateTraceJson(message, level, additionalDimensions);
            Console.WriteLine(log);
        }

        public string GetRequestId()
        {
            return contextAccessor == null ?
                defaultLogDetails?.CorrelationId :
                LogDetailManager.GetServiceRequestId(contextAccessor);
        }

        public void UpdateDefaultCorrelationId(string correlationId)
        {
            defaultLogDetails.CorrelationId = correlationId;
        }

        private LogDetails GetLogDetails()
        {
            if (contextAccessor != null)
            {
                LogDetails logDetails = LogDetailManager.GetLogDetails(contextAccessor);
                if (logDetails != null)
                {
                    return logDetails;
                }
            }

            return defaultLogDetails;
        }
    }
}
