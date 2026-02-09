using System.Diagnostics;
using System.Text;

namespace TranscriptionApi.Middleware;

/// <summary>
/// Middleware that logs all HTTP requests and responses with timing information.
/// Provides structured logging with request/response details for monitoring and debugging.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to log request and response information.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for WebSocket requests (they're handled separately)
        if (context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        // Log incoming request
        await LogRequest(context, requestId);

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Use a memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            stopwatch.Stop();

            // Log outgoing response
            await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

            // Copy the response back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Request {RequestId} failed after {ElapsedMs}ms - {Method} {Path}",
                requestId,
                stopwatch.ElapsedMilliseconds,
                context.Request.Method,
                context.Request.Path
            );
            
            throw;
        }
        finally
        {
            // Restore the original response body stream
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Logs details about the incoming HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="requestId">Unique identifier for this request.</param>
    private async Task LogRequest(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        // Build request information
        var requestInfo = new StringBuilder();
        requestInfo.AppendLine($"Request {requestId} started:");
        requestInfo.AppendLine($"  Method: {request.Method}");
        requestInfo.AppendLine($"  Path: {request.Path}");
        requestInfo.AppendLine($"  QueryString: {request.QueryString}");
        requestInfo.AppendLine($"  ContentType: {request.ContentType}");
        requestInfo.AppendLine($"  ContentLength: {request.ContentLength}");
        
        // Log headers (excluding sensitive ones)
        if (request.Headers.Any())
        {
            requestInfo.AppendLine("  Headers:");
            foreach (var header in request.Headers)
            {
                // Skip sensitive headers
                if (IsSensitiveHeader(header.Key))
                    continue;
                    
                requestInfo.AppendLine($"    {header.Key}: {header.Value}");
            }
        }

        _logger.LogInformation(
            "Request started - {RequestId} {Method} {Path}{QueryString} - ContentType: {ContentType}, ContentLength: {ContentLength}",
            requestId,
            request.Method,
            request.Path,
            request.QueryString,
            request.ContentType ?? "none",
            request.ContentLength?.ToString() ?? "unknown"
        );

        // For multipart form data, log file information
        if (request.HasFormContentType && request.Form.Files.Any())
        {
            foreach (var file in request.Form.Files)
            {
                _logger.LogInformation(
                    "Request {RequestId} includes file: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}",
                    requestId,
                    file.FileName,
                    file.Length,
                    file.ContentType
                );
            }
        }
    }

    /// <summary>
    /// Logs details about the outgoing HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="requestId">Unique identifier for this request.</param>
    /// <param name="elapsedMs">Time taken to process the request in milliseconds.</param>
    private async Task LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        var response = context.Response;
        
        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                       response.StatusCode >= 400 ? LogLevel.Warning :
                       LogLevel.Information;

        _logger.Log(
            logLevel,
            "Request completed - {RequestId} {Method} {Path} - Status: {StatusCode}, Duration: {ElapsedMs}ms, ContentType: {ContentType}, ContentLength: {ContentLength}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            response.StatusCode,
            elapsedMs,
            response.ContentType ?? "none",
            response.ContentLength?.ToString() ?? "unknown"
        );

        // Log slow requests as warnings
        if (elapsedMs > 5000) // 5 seconds
        {
            _logger.LogWarning(
                "Slow request detected - {RequestId} {Method} {Path} took {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                elapsedMs
            );
        }
    }

    /// <summary>
    /// Determines if a header contains sensitive information that should not be logged.
    /// </summary>
    /// <param name="headerName">The name of the header.</param>
    /// <returns>True if the header is sensitive, false otherwise.</returns>
    private bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}
