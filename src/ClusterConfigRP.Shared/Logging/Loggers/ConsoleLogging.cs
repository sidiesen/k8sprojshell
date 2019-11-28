// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleLogging.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Loggers
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Logging.Utilities;

    public class ConsoleLogging : ILogging
    {
        private readonly IHttpContextAccessor contextAccessor;
        private LogDetails defaultLogDetails;

        public ConsoleLogging(IHttpContextAccessor contextAccessor, LogDetails defaultLogDetails)
            : this(defaultLogDetails)
        {
            this.contextAccessor = contextAccessor;
        }

        public ConsoleLogging(LogDetails defaultLogDetails)
        {
            this.defaultLogDetails = defaultLogDetails;
            this.defaultLogDetails.CorrelationId = Guid.NewGuid().ToString();
        }

        public void TrackEvent(Enum eventName, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateEventJson(eventName, level, additionalDimensions);
            Console.WriteLine($"[{DateTime.UtcNow}] {log}");
        }

        public void TrackException(Exception e, string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateExceptionJson(e, message, level, additionalDimensions);
            Console.WriteLine($"[{DateTime.UtcNow}] {log}");
        }

        public void TrackMetric(string name, long value, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string metricDimensions = logDetails.GenerateMetricDimensions(additionalDimensions);
            Console.WriteLine($"[{DateTime.UtcNow}] name: {name}; value: {value}; dimensions: {metricDimensions}");
        }

        public void TrackTrace(string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateTraceJson(message, level, additionalDimensions);
            Console.WriteLine($"[{DateTime.UtcNow}] {log}");
        }

        public void UpdateDefaultCorrelationId(string correlationId)
        {
            defaultLogDetails.CorrelationId = correlationId;
        }

        public string GetRequestId()
        {
            return contextAccessor == null ?
                defaultLogDetails?.CorrelationId :
                LogDetailManager.GetServiceRequestId(contextAccessor);
        }

        private LogDetails GetLogDetails()
        {
            if (contextAccessor != null)
            {
                LogDetails LogDetails = LogDetailManager.GetLogDetails(contextAccessor);
                if (LogDetails != null)
                {
                    return LogDetails;
                }
            }

            return defaultLogDetails;
        }
    }
}
