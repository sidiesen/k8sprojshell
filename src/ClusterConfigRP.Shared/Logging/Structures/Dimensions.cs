// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared.Logging.Structures
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class Dimensions : ICustomDimensions
    {
        public Guid CorrelationId { get; }
        public string ClientRequestId { get; }
        public string TenantId {get; }

        [JsonConstructor]
        public Dimensions(string correlationId, string tenantId, string clientRequestId = null)
        {
            this.CorrelationId = new Guid();
            
            if (!String.IsNullOrWhiteSpace(correlationId) && Guid.TryParse(correlationId, out var parsedGuid))
            {
                this.CorrelationId = parsedGuid;
            }

            this.ClientRequestId = clientRequestId;
            this.TenantId = tenantId;
        }

        public IEnumerable<string> GetDimensions()
        {
            yield return $"\"{nameof(CorrelationId)}\":\"{CorrelationId}\"";
            if (!string.IsNullOrEmpty(ClientRequestId))
                yield return $"\"{nameof(ClientRequestId)}\":\"{ClientRequestId}\"";
            if (!string.IsNullOrEmpty(TenantId))
                yield return $"\"{nameof(TenantId)}\":\"{TenantId}\"";
        }
    }
}
