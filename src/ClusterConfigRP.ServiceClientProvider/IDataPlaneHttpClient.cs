//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using ClusterConfigRP.Shared.Logging.Structures;
    public interface IDataPlaneHttpClient
    {
        Task<HttpResponseMessage> PutAsync<TOutput, TInput>(Uri uri, TInput entity, Dimensions dimensions, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> GetWithRetryAsync(Uri uri, Dimensions dimensions, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> DeleteWithRetriesAsync(Uri uri, Dimensions dimensions, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> PostAsync<TInput>(Uri uri, TInput entity, Dimensions dimensions, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);
    }
}