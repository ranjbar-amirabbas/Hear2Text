namespace TranscriptionApi.Models;

/// <summary>
/// Response for the health check endpoint.
/// Indicates service status and model information.
/// </summary>
/// <param name="Status">Current status of the service (e.g., "healthy").</param>
/// <param name="ModelLoaded">Whether the Whisper model is loaded and ready.</param>
/// <param name="ModelSize">The size of the Whisper model being used (e.g., "medium").</param>
public record HealthResponse(
    string Status,
    bool ModelLoaded,
    string ModelSize
);

/// <summary>
/// Response for the capacity check endpoint.
/// Provides information about current service load and availability.
/// </summary>
/// <param name="ActiveJobs">Number of jobs currently being processed.</param>
/// <param name="QueuedJobs">Number of jobs waiting to be processed.</param>
/// <param name="MaxWorkers">Maximum number of concurrent workers configured.</param>
/// <param name="MaxQueueSize">Maximum number of jobs that can be queued.</param>
/// <param name="AvailableCapacity">Number of additional jobs that can be accepted.</param>
/// <param name="AtCapacity">Whether the service is at capacity and cannot accept new jobs.</param>
public record CapacityResponse(
    int ActiveJobs,
    int QueuedJobs,
    int MaxWorkers,
    int MaxQueueSize,
    int AvailableCapacity,
    bool AtCapacity
);

/// <summary>
/// Response for batch transcription job submission.
/// Returns the job ID and initial status.
/// </summary>
/// <param name="JobId">Unique identifier for the created transcription job.</param>
/// <param name="Status">Initial status of the job (typically "pending").</param>
public record BatchTranscriptionResponse(
    string JobId,
    string Status
);

/// <summary>
/// Response for synchronous transcription requests.
/// Returns the transcription result immediately.
/// </summary>
/// <param name="Transcription">The transcribed text.</param>
/// <param name="Status">Status of the transcription (typically "completed").</param>
public record SyncTranscriptionResponse(
    string Transcription,
    string Status
);

/// <summary>
/// Response for job status queries.
/// Provides current status and results of a transcription job.
/// </summary>
/// <param name="JobId">Unique identifier of the job.</param>
/// <param name="Status">Current status: pending, processing, completed, or failed.</param>
/// <param name="Transcription">The transcribed text. Only present when status is completed.</param>
/// <param name="Error">Error message if the job failed. Only present when status is failed.</param>
public record JobStatusResponse(
    string JobId,
    string Status,
    string? Transcription,
    string? Error
);

/// <summary>
/// Message sent over WebSocket during streaming transcription.
/// Contains partial, final, or error messages.
/// </summary>
/// <param name="Type">Message type: "partial", "final", or "error".</param>
/// <param name="Text">The transcription text or error message.</param>
/// <param name="Timestamp">Optional timestamp for the message (Unix timestamp).</param>
public record StreamingMessage(
    string Type,
    string Text,
    double? Timestamp
);

/// <summary>
/// Standard error response wrapper.
/// All API errors return this format for consistency.
/// </summary>
/// <param name="Error">The error details.</param>
public record ErrorResponse(
    ErrorDetail Error
);

/// <summary>
/// Detailed error information.
/// Provides structured error data for API consumers.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g., "INVALID_AUDIO_FORMAT").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional additional context or debugging information.</param>
public record ErrorDetail(
    string Code,
    string Message,
    string? Details
);
