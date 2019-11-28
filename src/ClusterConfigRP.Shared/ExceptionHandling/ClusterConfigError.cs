// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.Shared
{
    using System;

    [Serializable]
    public class ClusterConfigError
    {
        public ErrorCode ClusterConfigErrorCode { get; set; }

        public string Message { get; set; }

        public ClusterConfigError(ErrorCode errorCode = ErrorCode.Unspecified, string message = "")
        {
            this.ClusterConfigErrorCode = errorCode;
            this.Message = message;
        }

        public ClusterConfigError(ClusterConfigError configError)
        {
            this.ClusterConfigErrorCode = configError.ClusterConfigErrorCode;
            this.Message = configError.Message;
        }

        public enum ErrorCode
        {
            Unspecified = 1000,
            ValidationFailed,
            CreationFailed,
            GetFailed,
            DeleteConfigurationFailed,
            DeleteAllConfigurationsFailed,
            ListFailed,
            UnexpectedResponse,
            InvalidContinuationTokenFormat,
            ReadConfigurationFailed
        }
    }
}