using Microsoft.AspNetCore.Diagnostics;
using TranscriptionApi.Exceptions;
using TranscriptionApi.Models;

namespace TranscriptionApi.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions and formats them
/// into consistent error responses with appropriate HTTP status codes.
/// Implements IExceptionHandler for ASP.NET Core 8.0+ exception handling pipeline.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to handle the exception by mapping it to an appropriate HTTP response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the exception was handled, false otherwise.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Map exception to HTTP status code and error details
        var (statusCode, errorCode, message, details) = MapException(exception);

        // Log the exception with appropriate level and context
        LogException(exception, statusCode, httpContext);

        // Set response status code
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        // Create error response
        var errorResponse = new ErrorResponse(
            new ErrorDetail(errorCode, message, details)
        );

        // Write JSON response
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        // Return true to indicate the exception was handled
        return true;
    }

    /// <summary>
    /// Maps an exception to HTTP status code, error code, message, and optional details.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>Tuple containing status code, error code, message, and details.</returns>
    private (int StatusCode, string ErrorCode, string Message, string? Details) MapException(Exception exception)
    {
        return exception switch
        {
            // 400 Bad Request
            ArgumentException argEx => (
                400,
                "INVALID_REQUEST",
                argEx.Message,
                argEx.ParamName != null ? $"Parameter: {argEx.ParamName}" : null
            ),

            // 404 Not Found
            JobNotFoundException jobNotFound => (
                404,
                "JOB_NOT_FOUND",
                jobNotFound.Message,
                null
            ),

            // 413 Payload Too Large
            FileTooLargeException fileTooLarge => (
                413,
                "FILE_TOO_LARGE",
                fileTooLarge.Message,
                $"File size: {fileTooLarge.FileSize} bytes, Max size: {fileTooLarge.MaxSize} bytes"
            ),

            // 415 Unsupported Media Type
            InvalidAudioFormatException invalidFormat => (
                415,
                "INVALID_AUDIO_FORMAT",
                invalidFormat.Message,
                null
            ),

            // 503 Service Unavailable
            ServiceAtCapacityException atCapacity => (
                503,
                "SERVICE_AT_CAPACITY",
                atCapacity.Message,
                null
            ),

            // 500 Internal Server Error - Specific cases
            InvalidOperationException invalidOp when invalidOp.Message.Contains("FFmpeg") => (
                500,
                "AUDIO_CONVERSION_FAILED",
                "Failed to convert audio file to required format",
                invalidOp.Message
            ),

            InvalidOperationException invalidOp when invalidOp.Message.Contains("model") || invalidOp.Message.Contains("Whisper") => (
                500,
                "MODEL_LOAD_FAILED",
                "Failed to load or use the Whisper model",
                invalidOp.Message
            ),

            InvalidOperationException invalidOp when invalidOp.Message.Contains("transcription") || invalidOp.Message.Contains("transcribe") => (
                500,
                "TRANSCRIPTION_FAILED",
                "An error occurred during the transcription process",
                invalidOp.Message
            ),

            // 500 Internal Server Error - Generic
            _ => (
                500,
                "INTERNAL_ERROR",
                "An unexpected error occurred while processing your request",
                // Don't expose internal details in production
                exception.GetType().Name
            )
        };
    }

    /// <summary>
    /// Logs the exception with appropriate context and severity level.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="statusCode">The HTTP status code being returned.</param>
    /// <param name="httpContext">The HTTP context for additional logging context.</param>
    private void LogException(Exception exception, int statusCode, HttpContext httpContext)
    {
        var requestPath = httpContext.Request.Path;
        var requestMethod = httpContext.Request.Method;
        var exceptionType = exception.GetType().Name;

        // Log client errors (4xx) as warnings, server errors (5xx) as errors
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Server error occurred. Type: {ExceptionType}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
                exceptionType,
                requestMethod,
                requestPath,
                statusCode
            );
        }
        else if (statusCode >= 400)
        {
            _logger.LogWarning(
                exception,
                "Client error occurred. Type: {ExceptionType}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
                exceptionType,
                requestMethod,
                requestPath,
                statusCode
            );
        }
        else
        {
            // Shouldn't happen in exception handler, but log as info just in case
            _logger.LogInformation(
                exception,
                "Exception handled. Type: {ExceptionType}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
                exceptionType,
                requestMethod,
                requestPath,
                statusCode
            );
        }
    }
}
