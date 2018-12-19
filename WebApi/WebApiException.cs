
using System;

namespace WebApi
{
    class ErrorMessages
    {
        public const string Unknown = "Unknown exception";
    }

    // Basic exception for our WebApi to allow us to differentiate exceptions
    public class WebApiException : Exception
    {
        public WebApiException() : base(ErrorMessages.Unknown) {}
        public WebApiException(string errorMessage) : base(errorMessage) {}
        public WebApiException(string errorMessage, Exception innerException) : base(errorMessage, innerException) {}
    }

    // Exception when we need consent for the WebApi -- it will include the authorization url necessary to get consent
    public class WebApiExceptionNeedConsent : WebApiException
    {
        public string AuthorizationUrl { get; } = null;

        public WebApiExceptionNeedConsent(string sAuthorizationUrl) : base(ErrorMessages.Unknown)
        {
            AuthorizationUrl = sAuthorizationUrl;
        }

        public WebApiExceptionNeedConsent(string sAuthorizationUrl, string errorMessage) : base(errorMessage)
        {
            AuthorizationUrl = sAuthorizationUrl;
        }

        public WebApiExceptionNeedConsent(string sAuthorizationUrl, string errorMessage, Exception innerException) : base(errorMessage, innerException)
        {
            AuthorizationUrl = sAuthorizationUrl;
        }
    }
}
