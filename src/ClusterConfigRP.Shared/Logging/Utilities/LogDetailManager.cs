// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Utilities
{
    using Microsoft.AspNetCore.Http;

    using ClusterConfigRP.Shared.Logging.Structures;

    public static class LogDetailManager
    {
        private const string ContextItemsIndexKey = "CustomLogDetails";

        public static LogDetails GetLogDetails(IHttpContextAccessor contextAccessor)
        {
            return GetLogDetails(contextAccessor?.HttpContext);
        }

        public static LogDetails GetLogDetails(HttpContext context)
        {
            if (context == null)
                return null;

            bool LogDetailsFound = context.Items.TryGetValue(ContextItemsIndexKey, out object result);
            if (LogDetailsFound)
            {
                return (LogDetails)result;
            }

            return null;
        }

        public static void SetLogDetails(HttpContext context, LogDetails logDetails)
        {
            context.Items[ContextItemsIndexKey] = logDetails;
        }

        public static string GetServiceRequestId(IHttpContextAccessor contextAccessor)
        {
            return GetServiceRequestId(contextAccessor?.HttpContext);
        }

        public static string GetServiceRequestId(HttpContext context)
        {
            var logDetails = GetLogDetails(context);
            return logDetails.ServiceRequestId;
        }
    }
}