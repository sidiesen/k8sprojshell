// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------
namespace ClusterConfigRP.Shared.Logging.Structures
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    public class LogDetails
    {
        public string ServiceRequestId { get; set; }    // Service generated, as per RP contract https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/common-api-details.md#common-api-response-details
        public string ArmRequestId { get; set; }        // ARM provided
        public string ClientRequestId { get; set; }     // Client provided (optional)
        public string CorrelationId { get; set; }       // For non-web API services

        public string GenerateTraceJson(string message, LogLevel logLevel, params ICustomDimensions[] additionalDimensions)
        {
            StringBuilder sb = new StringBuilder("{");

            sb.Append($"\"Message\":{JsonConvert.ToString(message)},");
            sb.Append($"\"LogType\":\"{LogType.Trace}\",");
            sb.Append($"\"LogLevel\":\"{logLevel}\",");

            foreach (var dimensions in additionalDimensions)
            {
                sb.AppendDimensions(dimensions);
            }
            AddGlobalDimensions(sb);
            AddCorrelationDimensions(sb);

            string trace = sb.ToString();
            trace = trace.TrimEnd(',', '\r', '\n');
            trace += "}";

            return trace;
        }

        public string GenerateExceptionJson(Exception e, string message, LogLevel logLevel, params ICustomDimensions[] additionalDimensions)
        {
            StringBuilder sb = new StringBuilder("{");

            sb.Append($"\"Exception\":{JsonConvert.ToString(e.ToString())},");
            sb.Append($"\"ExceptionType\":\"{e.GetType()}\",");
            sb.Append($"\"Message\":{JsonConvert.ToString(message)},");
            sb.Append($"\"LogType\":\"{LogType.Exception}\",");
            sb.Append($"\"LogLevel\":\"{logLevel}\",");

            foreach (var dimensions in additionalDimensions)
            {
                sb.AppendDimensions(dimensions);
            }
            AddGlobalDimensions(sb);
            AddCorrelationDimensions(sb);

            string trace = sb.ToString();
            trace = trace.TrimEnd(',', '\r', '\n');
            trace += "}";

            return trace;

        }

        public string GenerateEventJson(Enum eventName, LogLevel logLevel, params ICustomDimensions[] additionalDimensions)
        {
            StringBuilder sb = new StringBuilder("{");

            sb.Append($"\"Event\":\"{eventName}\",");
            sb.Append($"\"LogType\":\"{LogType.Event}\",");
            sb.Append($"\"LogLevel\":\"{logLevel}\",");

            foreach (var dimensions in additionalDimensions)
            {
                sb.AppendDimensions(dimensions);
            }
            AddGlobalDimensions(sb);
            AddCorrelationDimensions(sb);
            string trace = sb.ToString();
            trace = trace.TrimEnd(',', '\r', '\n');
            trace += "}";

            return trace;
        }

        public string GenerateMetricDimensions(params ICustomDimensions[] additionalDimensions)
        {
            StringBuilder sb = new StringBuilder("{");

            foreach (var dimensions in additionalDimensions)
            {
                sb.AppendDimensions(dimensions);
            }
            AddGlobalDimensions(sb);

            string trace = sb.ToString();
            trace = trace.TrimEnd(',', '\r', '\n');
            trace += "}";

            return trace;
        }

        private void AddGlobalDimensions(StringBuilder sb)
        {
            sb.Append($"\"{nameof(GlobalLogDimensions.Environment)}\":\"{GlobalLogDimensions.Instance.Environment}\",");
            sb.Append($"\"{nameof(GlobalLogDimensions.Role)}\":\"{GlobalLogDimensions.Instance.Role}\",");
            sb.Append($"\"{nameof(GlobalLogDimensions.Location)}\":\"{GlobalLogDimensions.Instance.Location}\",");
        }

        private void AddCorrelationDimensions(StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(ServiceRequestId))
                sb.Append($",\"{nameof(ServiceRequestId)}\":\"{ServiceRequestId}\"");
            if (!string.IsNullOrEmpty(ArmRequestId))
                sb.Append($",\"{nameof(ArmRequestId)}\":\"{ArmRequestId}\"");
            if (!string.IsNullOrEmpty(ClientRequestId))
                sb.Append($",\"{nameof(ClientRequestId)}\":\"{ClientRequestId}\"");
            if (!string.IsNullOrEmpty(CorrelationId))
                sb.Append($",\"{nameof(CorrelationId)}\":\"{CorrelationId}\"");
        }
    }

    public static class StringBuilderDimensionExtensions
    {
        public static void AppendDimensions(this StringBuilder sb, ICustomDimensions customDimensions)
        {
            foreach (string dimension in customDimensions.GetDimensions())
            {
                sb.Append($"{dimension},");
            }
        }
    }
}
