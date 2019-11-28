//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpClientAdapter : IDisposable
    {
        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientAdapter" /> class.
        /// This is used when client endpoint (e.g. ARM) does not need a certificate.
        /// </summary>
        public HttpClientAdapter()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };

            this.httpClient = new HttpClient(handler);
            this.httpClient.DefaultRequestHeaders.Accept.Clear();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.AcceptJson));

            // TODO: Get ActivityId from CallContext, instead of generating a GUID here
            this.httpClient.DefaultRequestHeaders.Add(Constants.ClientRequestIdHeader, new Guid().ToString("D"));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual async Task<HttpResponseMessage> GetAsync(
            Uri url,
            CancellationToken cancellationToken,
            Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            this.AddHeaders(headers, request);

            return await this.httpClient.SendAsync(request, cancellationToken);
        }

        public virtual async Task<HttpResponseMessage> DeleteAsync(
            Uri url,
            CancellationToken cancellationToken,
            string entity = null,
            Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            if (entity != null)
            {
                request.Content = new ObjectContent<string>(entity, new JsonMediaTypeFormatter());
            }

            this.AddHeaders(headers, request);

            return await this.httpClient.SendAsync(request, cancellationToken);
        }

        public virtual async Task<HttpResponseMessage> PutAsync<TInput>(Uri url, TInput entity, CancellationToken cancellationToken, Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Content = new ObjectContent<TInput>(entity, new JsonMediaTypeFormatter());
            this.AddHeaders(headers, request);

            return await this.httpClient.SendAsync(request, cancellationToken);
        }

        public virtual async Task<HttpResponseMessage> PostAsync<TInput>(Uri url, TInput entity, CancellationToken cancellationToken, Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new ObjectContent<TInput>(entity, new JsonMediaTypeFormatter());
            this.AddHeaders(headers, request);

            return await this.httpClient.SendAsync(request, cancellationToken);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (this.httpClient != null)
                {
                    this.httpClient.Dispose();
                    this.httpClient = null;
                }
            }
        }

        private void AddHeaders(Dictionary<string, string> headers, HttpRequestMessage request)
        {
            if (headers != null)
            {
                // For multiple headers, not the enum header value types.
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
        }
    }
}