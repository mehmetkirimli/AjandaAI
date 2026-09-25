// Tüm controller'ların döndüğü ortak response zarfıdır.
// Ham entity yerine her zaman DTO taşır (bkz. docs/conventions.md).
// ResultType iç kullanım içindir; HTTP status kodunu Api katmanındaki filter belirler.

using System.Text.Json.Serialization;

namespace AjandaAI.Application.Common;

/// <summary>Generic tipten bağımsız olarak ResultType'a erişim sağlar (ApiResponseFilter kullanır).</summary>
public interface IApiResponse
{
    ResultType ResultType { get; }
}

public class ApiResponse<T> : IApiResponse
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();

    [JsonIgnore]
    public ResultType ResultType { get; init; } = ResultType.Success;

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message, ResultType = ResultType.Success };

    public static ApiResponse<T> Created(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message, ResultType = ResultType.Created };

    public static ApiResponse<T> NoContent(string message = "") =>
        new() { Success = true, Message = message, ResultType = ResultType.NoContent };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new(), ResultType = ResultType.ValidationError };

    public static ApiResponse<T> NotFound(string message) =>
        new() { Success = false, Message = message, ResultType = ResultType.NotFound };

    public static ApiResponse<T> Conflict(string message) =>
        new() { Success = false, Message = message, ResultType = ResultType.Conflict };

    public static ApiResponse<T> Error(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new(), ResultType = ResultType.Error };
}
