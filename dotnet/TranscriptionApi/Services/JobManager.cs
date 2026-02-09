namespace TranscriptionApi.Services;

using System.Collections.Concurrent;
using TranscriptionApi.Models;

/// <summary>
/// Manages transcription jobs with thread-safe operations.
/// Uses ConcurrentDictionary for thread-safe job storage.
/// Implements IDisposable to properly clean up the cleanup timer.
/// </summary>
public class JobManager : IJobManager, IDisposable
{
    private readonly ConcurrentDictionary<string, TranscriptionJob> _jobs;
    private readonly AppConfiguration _config;
    private readonly ILogger<JobManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _cleanupTimer;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the JobManager class.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for resolving scoped services.</param>
    public JobManager(
        AppConfiguration config, 
        ILogger<JobManager> logger,
        IServiceProvider serviceProvider)
    {
        _jobs = new ConcurrentDictionary<string, TranscriptionJob>();
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Initialize cleanup timer to run every hour
        // First cleanup runs after 1 hour, then repeats every hour
        _cleanupTimer = new Timer(
            CleanupOldJobs,
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
        
        _logger.LogInformation(
            "JobManager initialized with cleanup timer. Jobs older than {MaxAgeHours} hours will be removed.",
            _config.JobCleanupMaxAgeHours);
    }

    /// <inheritdoc/>
    public string CreateJob()
    {
        // Generate a unique job ID using GUID
        var jobId = Guid.NewGuid().ToString();
        
        // Create a new job with pending status
        var job = new TranscriptionJob
        {
            JobId = jobId,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        // Store the job in the concurrent dictionary
        if (_jobs.TryAdd(jobId, job))
        {
            _logger.LogInformation(
                "Job lifecycle: CREATED - JobId: {JobId}, Status: {Status}, CreatedAt: {CreatedAt}",
                jobId,
                job.Status,
                job.CreatedAt);
            return jobId;
        }
        
        // This should never happen with GUID, but handle it just in case
        _logger.LogError("Failed to create job with ID: {JobId} - ID collision", jobId);
        throw new InvalidOperationException($"Failed to create job with ID: {jobId}");
    }

    /// <inheritdoc/>
    public TranscriptionJob? GetJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogDebug("Retrieved job with ID: {JobId}, Status: {Status}", jobId, job.Status);
            return job;
        }
        
        _logger.LogDebug("Job not found with ID: {JobId}", jobId);
        return null;
    }

    /// <inheritdoc/>
    public void UpdateJobStatus(string jobId, JobStatus status, string? result = null, string? error = null)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("Attempted to update non-existent job: {JobId}", jobId);
            return;
        }

        // Update job status and related fields in a thread-safe manner
        var oldStatus = job.Status;
        job.Status = status;
        
        // Set completion timestamp for terminal states
        if (status == JobStatus.Completed || status == JobStatus.Failed)
        {
            job.CompletedAt = DateTime.UtcNow;
            var duration = job.CompletedAt.Value - job.CreatedAt;
            
            _logger.LogInformation(
                "Job lifecycle: {NewStatus} - JobId: {JobId}, OldStatus: {OldStatus}, Duration: {Duration}ms, CompletedAt: {CompletedAt}",
                status == JobStatus.Completed ? "COMPLETED" : "FAILED",
                jobId,
                oldStatus,
                duration.TotalMilliseconds,
                job.CompletedAt);
        }
        else
        {
            _logger.LogInformation(
                "Job lifecycle: STATUS_CHANGE - JobId: {JobId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
                jobId,
                oldStatus,
                status);
        }
        
        // Set transcription result for completed jobs
        if (status == JobStatus.Completed && result != null)
        {
            job.Transcription = result;
            _logger.LogInformation(
                "Job lifecycle: TRANSCRIPTION_SET - JobId: {JobId}, TranscriptionLength: {Length} characters",
                jobId,
                result.Length);
        }
        
        // Set error message for failed jobs
        if (status == JobStatus.Failed && error != null)
        {
            job.Error = error;
            _logger.LogError(
                "Job lifecycle: ERROR_SET - JobId: {JobId}, Error: {Error}",
                jobId,
                error);
        }
    }

    /// <inheritdoc/>
    public int GetActiveJobCount()
    {
        var count = _jobs.Values.Count(j => j.Status == JobStatus.Processing);
        _logger.LogDebug("Active job count: {Count}", count);
        return count;
    }

    /// <inheritdoc/>
    public int GetQueuedJobCount()
    {
        var count = _jobs.Values.Count(j => j.Status == JobStatus.Pending);
        _logger.LogDebug("Queued job count: {Count}", count);
        return count;
    }

    /// <inheritdoc/>
    public bool IsAtCapacity()
    {
        var activeJobs = GetActiveJobCount();
        var queuedJobs = GetQueuedJobCount();
        var totalJobs = activeJobs + queuedJobs;
        
        // At capacity if total jobs (active + queued) >= max queue size
        var atCapacity = totalJobs >= _config.MaxQueueSize;
        
        _logger.LogDebug(
            "Capacity check - Active: {Active}, Queued: {Queued}, Total: {Total}, Max: {Max}, AtCapacity: {AtCapacity}",
            activeJobs,
            queuedJobs,
            totalJobs,
            _config.MaxQueueSize,
            atCapacity);
        
        return atCapacity;
    }

    /// <inheritdoc/>
    public async Task StartJobProcessingAsync(string jobId, string audioFilePath)
    {
        _logger.LogInformation(
            "Job lifecycle: PROCESSING_STARTED - JobId: {JobId}, AudioFile: {AudioFilePath}",
            jobId,
            audioFilePath);
        
        string? convertedFilePath = null;
        var processingStartTime = DateTime.UtcNow;
        
        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var audioProcessor = scope.ServiceProvider.GetRequiredService<IAudioProcessor>();
        var whisperModelService = scope.ServiceProvider.GetRequiredService<IWhisperModelService>();
        
        try
        {
            // Step 1: Update job status to Processing
            UpdateJobStatus(jobId, JobStatus.Processing);
            
            // Step 2: Convert audio to Whisper format (16kHz mono WAV)
            _logger.LogDebug(
                "Job lifecycle: AUDIO_CONVERSION_STARTED - JobId: {JobId}, InputFile: {InputFile}",
                jobId,
                audioFilePath);
            
            var conversionStartTime = DateTime.UtcNow;
            convertedFilePath = await audioProcessor.ConvertToWhisperFormatAsync(audioFilePath, CancellationToken.None);
            var conversionDuration = DateTime.UtcNow - conversionStartTime;
            
            _logger.LogInformation(
                "Job lifecycle: AUDIO_CONVERSION_COMPLETED - JobId: {JobId}, OutputFile: {OutputFile}, Duration: {Duration}ms",
                jobId,
                convertedFilePath,
                conversionDuration.TotalMilliseconds);
            
            // Step 3: Transcribe using Whisper model
            _logger.LogDebug(
                "Job lifecycle: TRANSCRIPTION_STARTED - JobId: {JobId}, ConvertedFile: {ConvertedFile}",
                jobId,
                convertedFilePath);
            
            var transcriptionStartTime = DateTime.UtcNow;
            var transcription = await whisperModelService.TranscribeAsync(convertedFilePath, CancellationToken.None);
            var transcriptionDuration = DateTime.UtcNow - transcriptionStartTime;
            
            _logger.LogInformation(
                "Job lifecycle: TRANSCRIPTION_COMPLETED - JobId: {JobId}, Duration: {Duration}ms, ResultLength: {Length} characters",
                jobId,
                transcriptionDuration.TotalMilliseconds,
                transcription.Length);
            
            // Step 4: Update job with successful result
            UpdateJobStatus(jobId, JobStatus.Completed, result: transcription);
            
            var totalDuration = DateTime.UtcNow - processingStartTime;
            _logger.LogInformation(
                "Job lifecycle: PROCESSING_COMPLETED - JobId: {JobId}, TotalDuration: {Duration}ms",
                jobId,
                totalDuration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            // Step 4 (error path): Update job with error
            var totalDuration = DateTime.UtcNow - processingStartTime;
            _logger.LogError(
                ex,
                "Job lifecycle: PROCESSING_FAILED - JobId: {JobId}, Error: {ErrorMessage}, Duration: {Duration}ms",
                jobId,
                ex.Message,
                totalDuration.TotalMilliseconds);
            
            UpdateJobStatus(jobId, JobStatus.Failed, error: ex.Message);
        }
        finally
        {
            // Step 5: Clean up audio files
            _logger.LogDebug(
                "Job lifecycle: CLEANUP_STARTED - JobId: {JobId}",
                jobId);
            
            try
            {
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                    _logger.LogDebug(
                        "Job lifecycle: CLEANUP_FILE_DELETED - JobId: {JobId}, File: {FilePath}",
                        jobId,
                        audioFilePath);
                }
                
                if (convertedFilePath != null && File.Exists(convertedFilePath))
                {
                    File.Delete(convertedFilePath);
                    _logger.LogDebug(
                        "Job lifecycle: CLEANUP_FILE_DELETED - JobId: {JobId}, File: {FilePath}",
                        jobId,
                        convertedFilePath);
                }
                
                _logger.LogDebug(
                    "Job lifecycle: CLEANUP_COMPLETED - JobId: {JobId}",
                    jobId);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(
                    cleanupEx,
                    "Job lifecycle: CLEANUP_FAILED - JobId: {JobId}, Error: {ErrorMessage}",
                    jobId,
                    cleanupEx.Message);
            }
        }
    }

    /// <summary>
    /// Cleanup callback that removes jobs older than the configured maximum age.
    /// This method is called periodically by the cleanup timer.
    /// </summary>
    /// <param name="state">Timer state (not used).</param>
    private void CleanupOldJobs(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-_config.JobCleanupMaxAgeHours);
            var jobsToRemove = new List<string>();
            
            // Find jobs that are completed or failed and older than the cutoff time
            foreach (var kvp in _jobs)
            {
                var job = kvp.Value;
                
                // Only clean up terminal states (Completed or Failed)
                if ((job.Status == JobStatus.Completed || job.Status == JobStatus.Failed) &&
                    job.CompletedAt.HasValue &&
                    job.CompletedAt.Value < cutoffTime)
                {
                    jobsToRemove.Add(kvp.Key);
                }
            }
            
            // Remove the old jobs
            var removedCount = 0;
            foreach (var jobId in jobsToRemove)
            {
                if (_jobs.TryRemove(jobId, out var removedJob))
                {
                    removedCount++;
                    _logger.LogDebug(
                        "Removed old job {JobId} with status {Status}, completed at {CompletedAt}",
                        jobId,
                        removedJob.Status,
                        removedJob.CompletedAt);
                }
            }
            
            if (removedCount > 0)
            {
                _logger.LogInformation(
                    "Cleanup completed: Removed {RemovedCount} jobs older than {MaxAgeHours} hours. Remaining jobs: {RemainingCount}",
                    removedCount,
                    _config.JobCleanupMaxAgeHours,
                    _jobs.Count);
            }
            else
            {
                _logger.LogDebug(
                    "Cleanup completed: No jobs to remove. Current job count: {JobCount}",
                    _jobs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job cleanup: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Disposes the JobManager and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _cleanupTimer?.Dispose();
                _logger.LogInformation("JobManager disposed and cleanup timer stopped.");
            }
            
            _disposed = true;
        }
    }
}
