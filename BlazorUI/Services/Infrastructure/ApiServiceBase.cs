using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlazorUI.Models.Common;

namespace BlazorUI.Services.Infrastructure;

public abstract class ApiServiceBase(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected HttpClient Http => httpClient;

    protected async Task<ApiResult<T>> GetAsync<T>(
        string uri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(uri, cancellationToken);
        return await ParseResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<T>> PostAsync<T>(
        string uri, object? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(uri, content, JsonOptions, cancellationToken);
        return await ParseResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult> PostAsync(
        string uri, object? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(uri, content, JsonOptions, cancellationToken);
        return await ParseVoidResponseAsync(response, cancellationToken);
    }

    protected async Task<ApiResult> PutAsync(
        string uri, object? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PutAsJsonAsync(uri, content, JsonOptions, cancellationToken);
        return await ParseVoidResponseAsync(response, cancellationToken);
    }

    protected async Task<ApiResult> DeleteAsync(
        string uri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.DeleteAsync(uri, cancellationToken);
        return await ParseVoidResponseAsync(response, cancellationToken);
    }

    protected async Task<ApiResult<T>> PutWithResponseAsync<T>(
        string uri, object? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PutAsJsonAsync(uri, content, JsonOptions, cancellationToken);
        return await ParseResponseAsync<T>(response, cancellationToken);
    }

    private static async Task<ApiResult<T>> ParseResponseAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return ApiResult<T>.Success(value!, statusCode);
        }

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult<T>.Failure(problem, statusCode);
    }

    private static async Task<ApiResult> ParseVoidResponseAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            return ApiResult.Success(statusCode);
        }

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    private static async Task<ApiProblemDetails> TryReadProblemAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(JsonOptions, cancellationToken);
            if (problem is not null)
                return problem;
        }
        catch (JsonException)
        {
        }

        return new ApiProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)response.StatusCode}",
            Title = response.ReasonPhrase ?? "Error",
            Status = (int)response.StatusCode,
            Detail = response.StatusCode == HttpStatusCode.Unauthorized
                ? "You are not authenticated. Please sign in."
                : $"Request failed with status {(int)response.StatusCode}."
        };
    }

    protected static string BuildQueryString(params (string key, string? value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.value is not null)
            .Select(p => $"{Uri.EscapeDataString(p.key)}={Uri.EscapeDataString(p.value!)}");

        var query = string.Join("&", parts);
        return string.IsNullOrEmpty(query) ? string.Empty : $"?{query}";
    }
}
