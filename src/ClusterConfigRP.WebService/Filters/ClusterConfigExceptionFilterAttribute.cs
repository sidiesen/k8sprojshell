//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.Filters
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Net;
    using ClusterConfigRP.Shared;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class ClusterConfigExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext exceptionContext)
        {
            var clusterConfigError = new ClusterConfigError();
            string errorMessage;

            // Handle ValidationException case
            if (exceptionContext.Exception is ValidationException validationException)
            {
                errorMessage = string.Format(CultureInfo.InvariantCulture,
                                            "Validation error. [ErrorCode={0}][ErrorMessage={1}][ValidationException={2}]",
                                            ClusterConfigError.ErrorCode.ValidationFailed,
                                            validationException.Message,
                                            validationException);
                Console.WriteLine(errorMessage);

                exceptionContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                clusterConfigError.Message = "Validation failed!";
            }
            else if(exceptionContext.Exception is ClusterConfigException clusterConfigException)
            { 
                clusterConfigError = clusterConfigException.Error;

                // Construct the appropriate exception response
                errorMessage = clusterConfigError.Message;

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                "ClusterConfigurationError. [ErrorCode={0}][ErrorMessage={1}]",
                                                clusterConfigError.ClusterConfigErrorCode,
                                                errorMessage));

                exceptionContext.HttpContext.Response.StatusCode = (int)clusterConfigException.HttpStatusCode;
            }
            else
            {
                // An unhandled/unknown exception
                clusterConfigError.ClusterConfigErrorCode = ClusterConfigError.ErrorCode.Unspecified;

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                                "ClusterConfigurationError. [ErrorCode={0}][ErrorMessage={1}]",
                                                clusterConfigError.ClusterConfigErrorCode,
                                                exceptionContext.Exception.Message));

                exceptionContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            exceptionContext.Result = new JsonResult(clusterConfigError);

            base.OnException(exceptionContext);
        }
    }

    public class CCException
    {
        public HttpStatusCode statusCode { get; set; }
        public string Message { get; set; }
    }
}