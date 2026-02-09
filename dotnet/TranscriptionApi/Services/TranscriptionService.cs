namespace TranscriptionApi.Services;

using Microsoft.Extensions.Logging;
using TranscriptionApi.Models;

/// <summary>
/// Implementation of the transcription service.
/// Coordinates audio processing, worker concurrency, and Whisper model transcription.
/// </summary>
public class TranscriptionService : ITranscriptionService
{
    private readonly IWhisperModelService _modelService;
    private readonly IAudioProcessor _audioProcessor;
    private readonly ILogger<TranscriptionService> _logger;
    private readonly SemaphoreSlim _workerSemaphore;
    
    /// <summary>
    /// Initializes a new instance of the TranscriptionService class.
    /// </summary>
    /// <param name="modelService">Whisper model service for transcription</param>
    /// <param name="audioProcessor">Audio processor for file conversion</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Application configuration</param>
    public TranscriptionService(
        IWhisperModelService modelService,
        IAudioProcessor audioProcessor,
        ILogger<TranscriptionService> logger,
        AppConfiguration config)
    {
        _modelService = modelService;
        _audioProcessor = audioProcessor;
        _logger = logger;
        
        // Initialize semaphore with MaxConcurrentWorkers to limit concurrent transcriptions
        _workerSemaphore = new SemaphoreSlim(config.MaxConcurrentWorkers, config.MaxConcurrentWorkers);
        
        _logger.LogInformation(
            "TranscriptionService initialized with {MaxWorkers} concurrent workers",
            config.MaxConcurrentWorkers);
    }
    
    /// <inheritdoc/>
    public bool IsModelLoaded => _modelService.IsLoaded;
    
    /// <inheritdoc/>
    public string ModelSize => _modelService.ModelSize;
    
    /// <inheritdoc/>
    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken ct)
    {
        string? convertedFilePath = null;
        
        try
        {
            _logger.LogInformation("Starting batch transcription for audio file: {AudioFilePath}", audioFilePath);
            
            // Step 1: Convert audio to Whisper format (16kHz mono WAV)
            _logger.LogDebug("Converting audio file to Whisper format: {AudioFilePath}", audioFilePath);
            convertedFilePath = await _audioProcessor.ConvertToWhisperFormatAsync(audioFilePath, ct);
            _logger.LogInformation("Audio conversion completed: {ConvertedFilePath}", convertedFilePath);
            
            // Step 2: Acquire worker slot (blocks if all workers are busy)
            _logger.LogDebug("Acquiring worker slot for transcription");
            await _workerSemaphore.WaitAsync(ct);
            _logger.LogDebug("Worker slot acquired, starting transcription");
            
            try
            {
                // Step 3: Transcribe using Whisper model
                var transcription = await _modelService.TranscribeAsync(convertedFilePath, ct);
                _logger.LogInformation(
                    "Transcription completed successfully. Length: {Length} characters",
                    transcription.Length);
                
                return transcription;
            }
            finally
            {
                // Step 4: Release worker slot (always release, even on error)
                _workerSemaphore.Release();
                _logger.LogDebug("Worker slot released");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Transcription cancelled for audio file: {AudioFilePath}", audioFilePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for audio file: {AudioFilePath}", audioFilePath);
            throw new InvalidOperationException($"Transcription failed: {ex.Message}", ex);
        }
        finally
        {
            // Step 5: Clean up temporary converted file
            if (convertedFilePath != null)
            {
                try
                {
                    if (File.Exists(convertedFilePath))
                    {
                        File.Delete(convertedFilePath);
                        _logger.LogDebug("Deleted temporary converted file: {ConvertedFilePath}", convertedFilePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "Failed to clean up temporary file: {ConvertedFilePath}",
                        convertedFilePath);
                }
            }
        }
    }
    
    /// <inheritdoc/>
    public async Task<string> TranscribeStreamAsync(byte[] audioData, CancellationToken ct)
    {
        string? tempFilePath = null;
        string? convertedFilePath = null;
        
        try
        {
            _logger.LogInformation("Starting streaming transcription for {DataSize} bytes of audio data", audioData.Length);
            
            // Step 1: Save audio data to temporary file
            var tempPath = Path.GetTempPath();
            var fileName = $"stream_{Guid.NewGuid()}.wav";
            tempFilePath = Path.Combine(tempPath, fileName);
            
            _logger.LogDebug("Saving audio data to temporary file: {TempFilePath}", tempFilePath);
            await File.WriteAllBytesAsync(tempFilePath, audioData, ct);
            _logger.LogDebug("Audio data saved to temporary file");
            
            // Step 2: Convert to Whisper format (16kHz mono WAV)
            _logger.LogDebug("Converting audio data to Whisper format");
            convertedFilePath = await _audioProcessor.ConvertToWhisperFormatAsync(tempFilePath, ct);
            _logger.LogInformation("Audio conversion completed: {ConvertedFilePath}", convertedFilePath);
            
            // Step 3: Acquire worker slot (blocks if all workers are busy)
            _logger.LogDebug("Acquiring worker slot for streaming transcription");
            await _workerSemaphore.WaitAsync(ct);
            _logger.LogDebug("Worker slot acquired, starting transcription");
            
            try
            {
                // Step 4: Transcribe using Whisper model
                var transcription = await _modelService.TranscribeAsync(convertedFilePath, ct);
                _logger.LogInformation(
                    "Streaming transcription completed successfully. Length: {Length} characters",
                    transcription.Length);
                
                return transcription;
            }
            finally
            {
                // Step 5: Release worker slot (always release, even on error)
                _workerSemaphore.Release();
                _logger.LogDebug("Worker slot released");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming transcription cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming transcription failed");
            throw new InvalidOperationException($"Streaming transcription failed: {ex.Message}", ex);
        }
        finally
        {
            // Step 6: Clean up temporary files
            try
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Deleted temporary audio file: {TempFilePath}", tempFilePath);
                }
                
                if (convertedFilePath != null && File.Exists(convertedFilePath))
                {
                    File.Delete(convertedFilePath);
                    _logger.LogDebug("Deleted temporary converted file: {ConvertedFilePath}", convertedFilePath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temporary files");
            }
        }
    }
}
