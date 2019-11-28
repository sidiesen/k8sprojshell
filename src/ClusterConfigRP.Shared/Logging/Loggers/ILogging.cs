// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Loggers
{
    using System;
    using ClusterConfigRP.Shared.Logging.Structures;

    public interface ILogging
    {
        void TrackEvent(Enum eventName, LogLevel level, params ICustomDimensions[] additionalDimensions);
        void TrackException(Exception e, string message, LogLevel level, params ICustomDimensions[] additionalDimensions);
        void TrackMetric(string name, long value, params ICustomDimensions[] additionalDimensions);
        void TrackTrace(string message, LogLevel level, params ICustomDimensions[] additionalDimensions);

        void UpdateDefaultCorrelationId(string correlationId);
        string GetRequestId();
    }
}
