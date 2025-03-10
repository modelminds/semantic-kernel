﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ImageGeneration;

/// <summary>
/// Azure OpenAI Image generation
/// <see herf="https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference#image-generation" />
/// </summary>
[Experimental("SKEXP0012")]
public sealed class AzureOpenAIImageGeneration : IImageGeneration
{
    private readonly OpenAIImageGenerationClientCore _core;

    /// <summary>
    /// Generation Image Operation path
    /// </summary>
    private const string GenerationImageOperation = "openai/images/generations:submit";

    /// <summary>
    /// Get Image Operation path
    /// </summary>
    private const string GetImageOperation = "openai/operations/images";

    /// <summary>
    /// Azure OpenAI REST API endpoint
    /// </summary>
    private readonly string _endpoint;

    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Maximum number of attempts to retrieve the image generation operation result.
    /// </summary>
    private readonly int _maxRetryCount;

    /// <summary>
    /// Azure OpenAI Endpoint ApiVersion
    /// </summary>
    private readonly string _apiVersion;

    /// <summary>
    /// Create a new instance of Azure OpenAI image generation service
    /// </summary>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The ILoggerFactory used to create a logger for logging. If null, no logging will be performed.</param>
    /// <param name="maxRetryCount"> Maximum number of attempts to retrieve the image generation operation result.</param>
    /// <param name="apiVersion">Azure OpenAI Endpoint ApiVersion</param>
    public AzureOpenAIImageGeneration(
        string? endpoint, string apiKey, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null, int? maxRetryCount = null, string? apiVersion = null)
    {
        Verify.NotNullOrWhiteSpace(apiKey);
        if (httpClient?.BaseAddress == null && string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException($"The {nameof(httpClient)}.{nameof(HttpClient.BaseAddress)} and {nameof(endpoint)} are both null or empty. Please ensure at least one is provided.");
        }

        // Defaults if not supplied
        maxRetryCount ??= 5;
        apiVersion ??= "2023-06-01-preview";

        this._core = new(httpClient, loggerFactory?.CreateLogger(typeof(AzureOpenAIImageGeneration)));

        this._endpoint = !string.IsNullOrEmpty(endpoint) ? endpoint! : httpClient!.BaseAddress!.AbsoluteUri;
        this._apiKey = apiKey;
        this._maxRetryCount = maxRetryCount.Value;
        this._apiVersion = apiVersion;
        this._core.AddAttribute(AIServiceExtensions.EndpointKey, endpoint);
        this._core.AddAttribute(AIServiceExtensions.ApiVersionKey, apiVersion);

        this._core.RequestCreated += (_, request) => request.Headers.Add("api-key", this._apiKey);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => this._core.Attributes;

    /// <inheritdoc/>
    public async Task<string> GenerateImageAsync(string description, int width, int height, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var operationId = await this.StartImageGenerationAsync(description, width, height, cancellationToken).ConfigureAwait(false);
        var result = await this.GetImageGenerationResultAsync(operationId, cancellationToken).ConfigureAwait(false);

        if (result.Result is null)
        {
            throw new KernelException("Azure OpenAI Image Generation null response");
        }

        if (result.Result.Images.Count == 0)
        {
            throw new KernelException("Azure OpenAI Image Generation result not found");
        }

        return result.Result.Images.First().Url;
    }

    /// <summary>
    /// Start an image generation task
    /// </summary>
    /// <param name="description">Image description</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns> The operationId that identifies the original image generation request. </returns>
    private async Task<string> StartImageGenerationAsync(string description, int width, int height, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(description);
        if (width != height || (width != 256 && width != 512 && width != 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "OpenAI can generate only square images of size 256x256, 512x512, or 1024x1024.");
        }

        var requestBody = JsonSerializer.Serialize(new ImageGenerationRequest
        {
            Prompt = description,
            Size = $"{width}x{height}",
            Count = 1
        });

        var uri = this.GetUri(GenerationImageOperation);
        var result = await this._core.ExecutePostRequestAsync<AzureOpenAIImageGenerationResponse>(uri, requestBody, cancellationToken).ConfigureAwait(false);

        if (result == null || string.IsNullOrWhiteSpace(result.Id))
        {
            throw new KernelException("Response not contains result");
        }

        return result.Id;
    }

    /// <summary>
    /// Retrieve the results of an image generation operation.
    /// </summary>
    /// <param name="operationId">The operationId that identifies the original image generation request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns></returns>
    private async Task<AzureOpenAIImageGenerationResponse> GetImageGenerationResultAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var operationLocation = this.GetUri(GetImageOperation, operationId);

        var retryCount = 0;

        while (true)
        {
            if (this._maxRetryCount == retryCount)
            {
                throw new KernelException("Reached maximum retry attempts");
            }

            using var response = await this._core.ExecuteRequestAsync(operationLocation, HttpMethod.Get, null, cancellationToken).ConfigureAwait(false);
            var responseJson = await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);
            var result = OpenAIImageGenerationClientCore.JsonDeserialize<AzureOpenAIImageGenerationResponse>(responseJson);

            if (result.Status.Equals(AzureOpenAIImageOperationStatus.Succeeded, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }
            else if (this.IsFailedOrCancelled(result.Status))
            {
                throw new KernelException($"Azure OpenAI image generation {result.Status}");
            }

            if (response.Headers.TryGetValues("retry-after", out var afterValues) && long.TryParse(afterValues.FirstOrDefault(), out var after))
            {
                await Task.Delay(TimeSpan.FromSeconds(after), cancellationToken).ConfigureAwait(false);
            }

            // increase retry count
            retryCount++;
        }
    }

    private string GetUri(string operation, params string[] parameters)
    {
        var uri = new Azure.Core.RequestUriBuilder();
        uri.Reset(new Uri(this._endpoint));
        uri.AppendPath(operation, false);
        foreach (var parameter in parameters)
        {
            uri.AppendPath("/" + parameter, false);
        }
        uri.AppendQuery("api-version", this._apiVersion);
        return uri.ToString();
    }

    private bool IsFailedOrCancelled(string status)
    {
        return status.Equals(AzureOpenAIImageOperationStatus.Failed, StringComparison.OrdinalIgnoreCase)
            || status.Equals(AzureOpenAIImageOperationStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
            || status.Equals(AzureOpenAIImageOperationStatus.Deleted, StringComparison.OrdinalIgnoreCase);
    }
}
