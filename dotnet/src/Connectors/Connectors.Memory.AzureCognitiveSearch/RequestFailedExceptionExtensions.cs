﻿// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using Azure;
using Microsoft.SemanticKernel.Http;

namespace Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;

/// <summary>
/// Provides extension methods for the <see cref="RequestFailedException"/> class.
/// </summary>
internal static class RequestFailedExceptionExtensions
{
    /// <summary>
    /// Converts a <see cref="RequestFailedException"/> to an <see cref="HttpOperationException"/>.
    /// </summary>
    /// <param name="exception">The original <see cref="RequestFailedException"/>.</param>
    /// <returns>An <see cref="HttpOperationException"/> instance.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "By design. See comment below.")]
    public static HttpOperationException ToHttpOperationException(this RequestFailedException exception)
    {
        const int NoResponseReceived = 0;

        string? responseContent = null;

        try
        {
            responseContent = exception.GetRawResponse()?.Content?.ToString();
        }
        catch { } // We want to suppress any exceptions that occur while reading the content, ensuring that an HttpOperationException is thrown instead.

        return new HttpOperationException(
            exception.Status == NoResponseReceived ? null : (HttpStatusCode?)exception.Status,
            responseContent,
            exception.Message,
            exception);
    }
}
