namespace TranscriptionApi.Models;

/// <summary>
/// Application configuration settings for the transcription service.
/// Maps to the "Transcription" section in appsettings.json.
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Whisper model size to use for transcription.
    /// Valid values: tiny, base, small, medium, large.
    /// Default: medium
    /// </summary>
    public string WhisperModelSize { get; set; } = "medium";
    
    /// <summary>
    /// Maximum number of concurrent transcription workers.
    /// Controls how many transcription jobs can run simultaneously.
    /// Default: 4
    /// </summary>
    public int MaxConcurrentWorkers { get; set; } = 4;
    
    /// <summary>
    /// Maximum number of jobs that can be queued.
    /// When this limit is reached, new job submissions will be rejected with 503.
    /// Default: 100
    /// </summary>
    public int MaxQueueSize { get; set; } = 100;
    
    /// <summary>
    /// Maximum audio file size in megabytes.
    /// Files larger than this will be rejected with 413.
    /// Default: 500
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 500;
    
    /// <summary>
    /// Maximum age in hours before completed jobs are automatically cleaned up.
    /// Default: 24
    /// </summary>
    public int JobCleanupMaxAgeHours { get; set; } = 24;
    
    /// <summary>
    /// Minimum chunk size in bytes for streaming transcription.
    /// Audio buffer must reach this size before transcription is triggered.
    /// Default: 102400 (100 KB)
    /// </summary>
    public int StreamMinChunkSize { get; set; } = 102400;
    
    /// <summary>
    /// Maximum buffer size in bytes for streaming transcription.
    /// If buffer exceeds this size, an error is returned.
    /// Default: 10485760 (10 MB)
    /// </summary>
    public int StreamMaxBufferSize { get; set; } = 10485760;
}
