// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /// <summary>
    /// The ClusterConfig exception is thrown for failures in the business logic.
    /// </summary>
    /// <remarks>Specific exceptions can be derived from this exception.</remarks>
    [Serializable]
    public class ClusterConfigException : Exception
    {
        private const string ClusterConfigErrorPropertyName = "ClusterConfigError";

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigException"/> class.
        /// </summary>
        /// <param name="message">
        /// A message for this instance of the exception.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public ClusterConfigException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.HttpStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigException"/> class.
        /// </summary>
        /// <param name="message">
        /// Message for this instance of the exception.
        /// </param>
        public ClusterConfigException(string message)
            : base(message)
        {
            this.HttpStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigException"/> class.
        /// </summary>
        /// <param name="clusterConfigActionError">The ClusterConfig error for this instance.</param>
        /// <param name="innerException">Inner exception for this instance of the exception.</param>
        public ClusterConfigException(ClusterConfigError clusterConfigActionError, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError, Exception innerException = null)
            : base(clusterConfigActionError.Message, innerException)
        {
            this.HttpStatusCode = httpStatusCode;
            this.Error = clusterConfigActionError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigException"/> class.
        /// </summary>
        /// <param name="serializationInfo">
        /// The serialization info.
        /// </param>
        /// <param name="streamingContext">
        /// The streaming context.
        /// </param>
        protected ClusterConfigException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            this.HttpStatusCode = HttpStatusCode.InternalServerError;
            this.Error = (ClusterConfigError)serializationInfo.GetValue(ClusterConfigErrorPropertyName, typeof(ClusterConfigError));
        }

        public ClusterConfigError Error { get; private set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException("SerializationInfo");
            }

            serializationInfo.AddValue(ClusterConfigErrorPropertyName, this.Error);

            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}
