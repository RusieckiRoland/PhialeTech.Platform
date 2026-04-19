using System;
using System.Net;

namespace PhialeTech.Components.Shared.Services
{
    public enum DemoRemoteQueryFailureKind
    {
        Unknown,
        Forbidden,
    }

    public sealed class DemoRemoteQueryException : Exception
    {
        public DemoRemoteQueryException(
            DemoRemoteQueryFailureKind failureKind,
            string message,
            HttpStatusCode? statusCode = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            FailureKind = failureKind;
            StatusCode = statusCode;
        }

        public DemoRemoteQueryFailureKind FailureKind { get; }

        public HttpStatusCode? StatusCode { get; }
    }
}
