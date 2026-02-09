namespace TranscriptionApi.Exceptions;

/// <summary>
/// Exception thrown when a requested transcription job ID does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class JobNotFoundException : Exception
{
    public string JobId { get; }

    public JobNotFoundException(string jobId)
        : base($"Job with ID '{jobId}' was not found")
    {
        JobId = jobId;
    }

    public JobNotFoundException(string jobId, string message, Exception innerException)
        : base(message, innerException)
    {
        JobId = jobId;
    }
}
