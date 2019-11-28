//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider.Entities
{
    using System.Net;

    public class ProviderResult<T>
    {
        public T Response { get; set; }

        public string Error { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }
    }
}
