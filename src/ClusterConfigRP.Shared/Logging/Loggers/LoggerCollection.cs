// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Loggers
{
    using System;
    using System.Collections.Generic;
    using ClusterConfigRP.Shared.Logging.Structures;


    public class LoggerCollection : ILogging
    {
        private readonly List<ILogging> loggerInstances;
        private LogLevel currentLogLevel;

        public LoggerCollection(LogLevel currentLogLevel)
        {
            this.currentLogLevel = currentLogLevel;
            loggerInstances = new List<ILogging>();
        }

        public void AddOn(ILogging logger)
        {
            loggerInstances.Add(logger);
        }

        public void AddOn(ICollection<ILogging> loggers)
        {
            foreach (var logger in loggers)
            {
                AddOn(logger);
            }
        }

        public void TrackEvent(Enum eventName, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            // If the log level of the intended message is not high enough, don't write it.
            if (level > currentLogLevel) return;

            foreach (ILogging logger in loggerInstances)
            {
                try
                {
                    logger.TrackEvent(eventName, level, additionalDimensions);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to execute TrackEvent on logger type: {logger.GetType()}. Exception: {e}");
                }
            }
        }

        public void TrackException(Exception e, string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            // If the log level of the intended message is not high enough, don't write it.
            if (level > currentLogLevel) return;

            foreach (ILogging logger in loggerInstances)
            {
                try
                {
                    logger.TrackException(e, message, level, additionalDimensions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to execute TrackException on logger type: {logger.GetType()}. Exception: {ex}");
                }
            }
        }

        public void TrackMetric(string name, long value, params ICustomDimensions[] additionalDimensions)
        {
            foreach (ILogging logger in loggerInstances)
            {
                try
                {
                    logger.TrackMetric(name, value, additionalDimensions);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to execute TrackMetric on logger type: {logger.GetType()}. Exception: {e}");
                }
            }
        }

        public void TrackTrace(string message, LogLevel level, params ICustomDimensions[] additionalDimensions)
        {
            // If the log level of the intended message is not high enough, don't write it.
            if (level > currentLogLevel) return;

            foreach (ILogging logger in loggerInstances)
            {
                try
                {
                    logger.TrackTrace(message, level, additionalDimensions);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to execute TrackTrace on logger type: {logger.GetType()}. Exception: {e}");
                }
            }
        }

        public void UpdateDefaultCorrelationId(string correlationId)
        {
            foreach (ILogging logger in loggerInstances)
            {
                logger.UpdateDefaultCorrelationId(correlationId);
            }
        }

        public string GetRequestId()
        {
            if (loggerInstances.Count > 0)
            {
                return loggerInstances[0].GetRequestId();
            }

            return string.Empty;
        }
    }
}
