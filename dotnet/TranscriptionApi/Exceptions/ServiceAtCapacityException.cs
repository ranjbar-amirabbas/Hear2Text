namespace TranscriptionApi.Exceptions;

/// <summary>
/// Exception thrown when the service cannot accept new transcription jobs due to capacity limits.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public class ServiceAtCapacityException : Exception
{
    public int ActiveJobs { get; }
    public int QueuedJobs { get; }
    public int MaxCapacity { get; }

    public ServiceAtCapacityException()
        : base("Service is at capacity. Please try again later.")
    {
    }

    public ServiceAtCapacityException(int activeJobs, int queuedJobs, int maxCapacity)
        : base($"Service is at capacity. Active jobs: {activeJobs}, Queued jobs: {queuedJobs}, Max capacity: {maxCapacity}")
    {
        ActiveJobs = activeJobs;
        QueuedJobs = queuedJobs;
        MaxCapacity = maxCapacity;
    }

    public ServiceAtCapacityException(string message)
        : base(message)
    {
    }

    public ServiceAtCapacityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
