// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLogging.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Loggers
{
    using System;
    using System.IO;

    using Microsoft.AspNetCore.Http;

    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Logging.Utilities;

    public class FileLogging : ILogging
    {
        private static readonly string logName = "ClusterConfigRP.log";
        private readonly string logPath;
        private readonly IHttpContextAccessor contextAccessor;
        private LogDetails defaultLogDetails;

        public FileLogging(IHttpContextAccessor contextAccessor, LogDetails defaultLogDetails)
            : this(defaultLogDetails)
        {
            this.contextAccessor = contextAccessor;
        }

        public FileLogging(LogDetails defaultLogDetails)
        {
            this.defaultLogDetails = defaultLogDetails;
            this.defaultLogDetails.CorrelationId = Guid.NewGuid().ToString();
            logPath = Path.Combine(Path.GetTempPath(), logName);
        }

        public void TrackEvent(Enum eventName, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateEventJson(eventName, level, additionalDimensions);
            Write($"[{DateTime.UtcNow}] {log}");
        }

        public void TrackException(Exception e, string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateExceptionJson(e, message, level, additionalDimensions);
            Write($"[{DateTime.UtcNow}] {log}");
        }

        public void TrackMetric(string name, long value, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string metricDimensions = logDetails.GenerateMetricDimensions(additionalDimensions);
            Write($"[{DateTime.UtcNow}] name: {name}; value: {value}; dimensions: {metricDimensions}");
        }

        public void TrackTrace(string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            var logDetails = GetLogDetails();
            string log = logDetails.GenerateTraceJson(message, level, additionalDimensions);
            Write($"[{DateTime.UtcNow}] {log}");
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

        private void Write(string message)
        {
            using StreamWriter logFile = new StreamWriter(logPath);
            logFile.WriteLine(message);
        }
    }
}

