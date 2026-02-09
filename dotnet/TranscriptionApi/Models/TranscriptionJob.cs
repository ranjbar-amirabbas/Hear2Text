namespace TranscriptionApi.Models;

/// <summary>
/// Represents the status of a transcription job.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job has been created and is waiting to be processed.
    /// </summary>
    Pending,
    
    /// <summary>
    /// Job is currently being processed.
    /// </summary>
    Processing,
    
    /// <summary>
    /// Job has completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// Job has failed due to an error.
    /// </summary>
    Failed
}

/// <summary>
/// Represents a transcription job with its current state and results.
/// </summary>
public class TranscriptionJob
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public required string JobId { get; set; }
    
    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; }
    
    /// <summary>
    /// The transcribed text. Only populated when Status is Completed.
    /// </summary>
    public string? Transcription { get; set; }
    
    /// <summary>
    /// Error message if the job failed. Only populated when Status is Failed.
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when the job completed (either successfully or with failure).
    /// Null if the job is still pending or processing.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
