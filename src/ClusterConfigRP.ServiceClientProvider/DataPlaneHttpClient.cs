//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.ServiceClientProvider
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using ClusterConfigRP.Shared.Logging.Structures;
    using ClusterConfigRP.Shared.Logging.Loggers;
    using ClusterConfigRP.Shared.Validation;
    using System.Globalization;

    public class DataPlaneHttpClient : IDataPlaneHttpClient, IDisposable
    {
        private readonly HttpClientAdapter httpClientAdapter;
        private ILogging logging;

        public DataPlaneHttpClient(HttpClientAdapter httpClientAdapter, ILogging logging)
        {
            if (httpClientAdapter == null)
            {
                throw new ArgumentNullException(nameof(httpClientAdapter));
            }

            if (logging == null)
            {
                throw new ArgumentNullException(nameof(logging));
            }

            this.httpClientAdapter = httpClientAdapter;
            this.logging = logging;
        }
        
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<HttpResponseMessage> GetWithRetryAsync(
            Uri requestUrl,
            Dimensions dimensions,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            Requires.Argument("requestUrl", requestUrl).NotNull();

            var stopwatch = Stopwatch.StartNew();
            var response = new HttpResponseMessage();
            
            try
            {
                var retries = new MillisecondRetryDelayArray(500, 1000, 1500);
                while (retries.NoneLeft == false)
                {
                    response = await this.httpClientAdapter.GetAsync(requestUrl, cancellationToken, headers);

                    if (response == null)
                    {
                        string message = "NULL response while retrieving Cluster Configuration!";
                        logging.TrackTrace(message, LogLevel.Error, dimensions);

                        throw new HttpRequestException(message);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    else
                    {
                        var responseContent = response.Content == null
                                    ? string.Empty
                                    : await response.Content.ReadAsStringAsync();

                        logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to GetConfiguration. StatusCode: {0}; ReasonPhrase: {1}.", response.StatusCode, response.ReasonPhrase), LogLevel.Error, dimensions);

                        if (retries.OnLastRetry)
                        {
                            logging.TrackTrace("No more retries left; exiting.", LogLevel.Verbose, dimensions);
                            break;
                        }

                        if (!IsRetryableServerSideError(response))
                        {
                            logging.TrackTrace("Non retryable Server Side error; exiting.", LogLevel.Verbose, dimensions);
                            break;
                        }

                        logging.TrackTrace("Retrying get.", LogLevel.Verbose, dimensions);

                        await retries.Delay(cancellationToken);
                    }

                    await retries.Delay(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Received an exception while getting the config.  Return BadRequest, so as to have the Client retry the operation
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to get; and received an exception: {0}.", ex.Message), LogLevel.Error, dimensions);

                response.StatusCode = HttpStatusCode.BadRequest;
                response.ReasonPhrase = "Failed to get configuration; please retry";
            }
            finally
            {
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Time elapsed in ms: {0}.", stopwatch.ElapsedMilliseconds), LogLevel.Verbose, dimensions);
            }

            return response;
        }

        public async Task<HttpResponseMessage> DeleteWithRetriesAsync(
            Uri requestUrl,
            Dimensions dimensions,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            Requires.Argument("requestUrl", requestUrl).NotNull();

            var stopwatch = Stopwatch.StartNew();
            var response = new HttpResponseMessage();

            try
            {
                var retries = new MillisecondRetryDelayArray(500, 1000, 1500);
                while (retries.NoneLeft == false)
                {
                    response = await this.httpClientAdapter.DeleteAsync(requestUrl, cancellationToken, headers: headers);

                    if (response == null)
                    {
                        string message = "NULL response while deleting Cluster Configuration!";
                        logging.TrackTrace(message, LogLevel.Error, dimensions);

                        throw new HttpRequestException(message);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    else
                    {
                        var responseContent = response.Content == null
                                    ? string.Empty
                                    : await response.Content.ReadAsStringAsync();

                        logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to delete; received response: {0}.", responseContent), LogLevel.Error, dimensions);

                        if (retries.OnLastRetry)
                        {
                            logging.TrackTrace("No more retries left; exiting.", LogLevel.Verbose, dimensions);
                            break;
                        }

                        if (!IsRetryableServerSideError(response))
                        {
                            logging.TrackTrace("Non retryable Server Side error; exiting.", LogLevel.Error, dimensions);
                            break;
                        }

                        logging.TrackTrace("Retrying delete.", LogLevel.Verbose, dimensions);

                        await retries.Delay(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // Received an exception while deleting the config.  Return BadRequest, so as to have the Client retry the operation
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to delete; and received an exception: {0}.", ex.Message), LogLevel.Error, dimensions);

                response.StatusCode = HttpStatusCode.BadRequest;
                response.ReasonPhrase = "Failed to delete configuration; please retry";
            }
            finally
            {
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Time elapsed in ms: {0}.", stopwatch.ElapsedMilliseconds), LogLevel.Verbose, dimensions);
            }

            return response;
        }

        public async Task<HttpResponseMessage> PutAsync<TOutput, TInput>(
            Uri requestUrl,
            TInput entity,
            Dimensions dimensions,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            Requires.Argument("requestUrl", requestUrl).NotNull();
            Requires.Argument("entity", entity).NotNull();

            var stopwatch = Stopwatch.StartNew();
            var response = new HttpResponseMessage();

            try
            {
                response = await this.httpClientAdapter.PutAsync(requestUrl,
                                                                 entity,
                                                                 cancellationToken,
                                                                 headers);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content == null
                                    ? string.Empty
                                    : await response.Content.ReadAsStringAsync();

                    logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to Create; StatusCode: {0}, Message: {1}.", response.StatusCode, responseContent), LogLevel.Error, dimensions);
                }
            }
            catch (Exception ex)
            {
                // Received an exception in put config.  Return BadRequest, so as to have the Client retry the operation
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to Create; and received an exception: {0}.", ex.Message), LogLevel.Error, dimensions);

                response.StatusCode = HttpStatusCode.BadRequest;
                response.ReasonPhrase = "Failed to create configuration; please retry";
            }
            finally
            {
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Time elapsed in ms: {0}.", stopwatch.ElapsedMilliseconds), LogLevel.Verbose, dimensions);
            }

            return response;
        }

        public async Task<HttpResponseMessage> PostAsync<TInput>(
            Uri requestUrl,
            TInput entity,
            Dimensions dimensions,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            Requires.Argument("requestUrl", requestUrl).NotNull();
            Requires.Argument("entity", entity).NotNull();

            var stopwatch = Stopwatch.StartNew();
            var response = new HttpResponseMessage();

            try
            {
                response = await this.httpClientAdapter.PostAsync(requestUrl,
                                                                 entity,
                                                                 cancellationToken,
                                                                 headers);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content == null
                                    ? string.Empty
                                    : await response.Content.ReadAsStringAsync();

                    logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to Post request; StatusCode: {0}, Message: {1}.", response.StatusCode, responseContent), LogLevel.Error, dimensions);
                }
            }
            catch (Exception ex)
            {
                // Received an exception in post request.  Return BadRequest, so as to have the Client retry the operation
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Failed to post request; and received an exception: {0}.", ex.Message), LogLevel.Error, dimensions);

                response.StatusCode = HttpStatusCode.BadRequest;
                response.ReasonPhrase = "Failed to post request; please retry";
            }
            finally
            {
                logging.TrackTrace(string.Format(CultureInfo.InvariantCulture, "Time elapsed in ms: {0}.", stopwatch.ElapsedMilliseconds), LogLevel.Verbose, dimensions);
            }

            return response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.httpClientAdapter != null)
                {
                    this.Dispose();
                }
            }
        }

        #region Private Members

        private static bool IsRetryableServerSideError(HttpResponseMessage response)
        {
            return response.StatusCode == HttpStatusCode.InternalServerError
                   || response.StatusCode == HttpStatusCode.ServiceUnavailable;
        }

        private class MillisecondRetryDelayArray
        {
            private readonly int[] milliseconds;

            private readonly int retryMaximum;

            public MillisecondRetryDelayArray(params int[] milliseconds)
            {
                this.milliseconds = milliseconds;
                this.CurrentRetry = -1;
                this.retryMaximum = this.milliseconds.Length;
            }

            public int CurrentRetry { get; private set; }

            public bool NoneLeft
            {
                get { return this.CurrentRetry >= this.retryMaximum; }
            }

            public bool OnLastRetry
            {
                get { return this.CurrentRetry >= (this.retryMaximum - 2); }
            }

            public async Task Delay(CancellationToken cancellationToken)
            {
                this.CurrentRetry++;
                if (this.NoneLeft == false)
                {
                    var delayMilliseconds = this.milliseconds[this.CurrentRetry];
                    await Task.Delay(delayMilliseconds, cancellationToken);
                }
            }
        }
        #endregion
    }
}