namespace TranscriptionApi.Services;

using TranscriptionApi.Models;

/// <summary>
/// Interface for managing transcription jobs.
/// Provides thread-safe job creation, retrieval, and status updates.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// Creates a new transcription job with a unique ID and pending status.
    /// </summary>
    /// <returns>The unique job ID for the newly created job.</returns>
    string CreateJob();
    
    /// <summary>
    /// Retrieves a job by its ID.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>The transcription job if found, null otherwise.</returns>
    TranscriptionJob? GetJob(string jobId);
    
    /// <summary>
    /// Updates the status of a job in a thread-safe manner.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <param name="status">The new status to set.</param>
    /// <param name="result">Optional transcription result (for completed jobs).</param>
    /// <param name="error">Optional error message (for failed jobs).</param>
    void UpdateJobStatus(string jobId, JobStatus status, string? result = null, string? error = null);
    
    /// <summary>
    /// Gets the count of jobs currently being processed.
    /// </summary>
    /// <returns>The number of jobs with Processing status.</returns>
    int GetActiveJobCount();
    
    /// <summary>
    /// Gets the count of jobs waiting to be processed.
    /// </summary>
    /// <returns>The number of jobs with Pending status.</returns>
    int GetQueuedJobCount();
    
    /// <summary>
    /// Checks if the service is at capacity and cannot accept new jobs.
    /// </summary>
    /// <returns>True if at capacity, false otherwise.</returns>
    bool IsAtCapacity();
    
    /// <summary>
    /// Starts asynchronous processing of a transcription job.
    /// Updates job status to Processing, performs transcription, and updates with result or error.
    /// Cleans up temporary audio files after processing.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <param name="audioFilePath">Path to the uploaded audio file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartJobProcessingAsync(string jobId, string audioFilePath);
}
