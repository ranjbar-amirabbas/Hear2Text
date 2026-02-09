namespace TranscriptionApi.Models;

/// <summary>
/// Validates AppConfiguration settings to ensure they meet requirements.
/// Should be called during application startup to fail fast on invalid configuration.
/// </summary>
public static class AppConfigurationValidator
{
    /// <summary>
    /// List of valid Whisper model sizes.
    /// </summary>
    private static readonly string[] ValidModelSizes = { "tiny", "base", "small", "medium", "large" };
    
    /// <summary>
    /// Validates the provided configuration and throws InvalidOperationException if any setting is invalid.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration contains invalid values.</exception>
    public static void Validate(AppConfiguration config)
    {
        if (config.MaxConcurrentWorkers < 1)
        {
            throw new InvalidOperationException(
                $"MaxConcurrentWorkers must be >= 1, but was {config.MaxConcurrentWorkers}");
        }
        
        if (config.MaxQueueSize < 1)
        {
            throw new InvalidOperationException(
                $"MaxQueueSize must be >= 1, but was {config.MaxQueueSize}");
        }
        
        if (config.MaxFileSizeMB < 1)
        {
            throw new InvalidOperationException(
                $"MaxFileSizeMB must be >= 1, but was {config.MaxFileSizeMB}");
        }
        
        if (!ValidModelSizes.Contains(config.WhisperModelSize.ToLowerInvariant()))
        {
            throw new InvalidOperationException(
                $"Invalid WhisperModelSize: '{config.WhisperModelSize}'. " +
                $"Valid values are: {string.Join(", ", ValidModelSizes)}");
        }
    }
}
