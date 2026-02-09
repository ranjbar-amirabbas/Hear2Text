namespace TranscriptionApi.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TranscriptionApi.Models;
using TranscriptionApi.Exceptions;
using FFMpegCore;

/// <summary>
/// Implementation of audio processing service.
/// Handles validation, storage, and conversion of audio files for transcription.
/// </summary>
public class AudioProcessor : IAudioProcessor
{
    private readonly ILogger<AudioProcessor> _logger;
    private readonly AppConfiguration _config;
    
    // Supported audio file extensions
    private static readonly string[] SupportedFormats = { ".wav", ".mp3", ".ogg", ".m4a" };
    
    // Supported MIME types for audio files
    private static readonly string[] SupportedMimeTypes = 
    { 
        "audio/wav", 
        "audio/wave", 
        "audio/x-wav",
        "audio/mpeg", 
        "audio/mp3",
        "audio/ogg", 
        "audio/vorbis",
        "audio/x-m4a",
        "audio/m4a",
        "audio/mp4"
    };
    
    public AudioProcessor(ILogger<AudioProcessor> logger, AppConfiguration config)
    {
        _logger = logger;
        _config = config;
    }
    
    /// <inheritdoc/>
    public bool IsValidAudioFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Validation failed: File is null or empty");
            throw new InvalidAudioFormatException("No file provided or file is empty");
        }
        
        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!SupportedFormats.Contains(extension))
        {
            _logger.LogWarning("Validation failed: Unsupported file extension {Extension}", extension);
            throw InvalidAudioFormatException.ForExtension(extension);
        }
        
        // Check file size
        var maxSizeBytes = (long)_config.MaxFileSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            _logger.LogWarning(
                "Validation failed: File size {FileSize} exceeds maximum {MaxSize}", 
                file.Length, 
                maxSizeBytes);
            throw new FileTooLargeException(file.Length, maxSizeBytes);
        }
        
        // Check MIME type
        if (!string.IsNullOrEmpty(file.ContentType))
        {
            var mimeType = file.ContentType.ToLowerInvariant();
            if (!SupportedMimeTypes.Contains(mimeType))
            {
                _logger.LogWarning(
                    "Validation failed: Unsupported MIME type {MimeType} for file {FileName}", 
                    mimeType, 
                    file.FileName);
                throw new InvalidAudioFormatException(
                    $"Unsupported MIME type: {mimeType}. Supported formats: WAV, MP3, OGG, M4A");
            }
        }
        
        _logger.LogInformation(
            "File validation successful: {FileName}, Size: {FileSize} bytes, Type: {ContentType}",
            file.FileName,
            file.Length,
            file.ContentType);
        
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<string> SaveUploadedFileAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            // Generate a unique temporary file path
            var tempPath = Path.GetTempPath();
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"upload_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(tempPath, fileName);
            
            _logger.LogInformation(
                "Audio processing: UPLOAD_SAVE_STARTED - FileName: {FileName}, Size: {FileSize} bytes, TargetPath: {FilePath}",
                file.FileName,
                file.Length,
                filePath);
            
            var saveStartTime = DateTime.UtcNow;
            
            // Save the file to disk
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, ct);
            }
            
            var saveDuration = DateTime.UtcNow - saveStartTime;
            
            _logger.LogInformation(
                "Audio processing: UPLOAD_SAVE_COMPLETED - FilePath: {FilePath}, Size: {FileSize} bytes, Duration: {Duration}ms",
                filePath,
                file.Length,
                saveDuration.TotalMilliseconds);
            
            return filePath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Audio processing: UPLOAD_SAVE_FAILED - FileName: {FileName}, Error: {ErrorMessage}",
                file.FileName,
                ex.Message);
            throw;
        }
    }
    
    /// <inheritdoc/>
    public async Task<string> ConvertToWhisperFormatAsync(string inputPath, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"Input audio file not found: {inputPath}");
            }
            
            var inputFileInfo = new FileInfo(inputPath);
            _logger.LogInformation(
                "Audio processing: CONVERSION_STARTED - InputFile: {InputPath}, Size: {FileSize} bytes",
                inputPath,
                inputFileInfo.Length);
            
            // Generate output file path
            var tempPath = Path.GetTempPath();
            var outputFileName = $"converted_{Guid.NewGuid()}.wav";
            var outputPath = Path.Combine(tempPath, outputFileName);
            
            _logger.LogDebug(
                "Audio processing: CONVERSION_CONFIG - OutputFile: {OutputPath}, TargetFormat: 16kHz mono WAV with normalization",
                outputPath);
            
            var conversionStartTime = DateTime.UtcNow;
            
            // Convert audio to 16kHz mono WAV with normalization
            // FFMpegCore uses FFmpeg under the hood
            _logger.LogDebug("Audio processing: FFMPEG_EXECUTION_STARTED - InputFile: {InputPath}", inputPath);
            
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")        // PCM 16-bit little-endian
                    .WithAudioSamplingRate(16000)       // 16kHz sample rate
                    .WithCustomArgument("-ac 1")        // Mono channel
                    .WithCustomArgument("-af loudnorm") // Audio normalization
                )
                .CancellableThrough(ct)
                .ProcessAsynchronously();
            
            var conversionDuration = DateTime.UtcNow - conversionStartTime;
            _logger.LogDebug(
                "Audio processing: FFMPEG_EXECUTION_COMPLETED - Duration: {Duration}ms",
                conversionDuration.TotalMilliseconds);
            
            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException(
                    $"FFmpeg conversion completed but output file not found: {outputPath}");
            }
            
            var outputFileInfo = new FileInfo(outputPath);
            _logger.LogInformation(
                "Audio processing: CONVERSION_COMPLETED - OutputFile: {OutputPath}, Size: {FileSize} bytes, Duration: {Duration}ms, SizeChange: {SizeChange}%",
                outputPath,
                outputFileInfo.Length,
                conversionDuration.TotalMilliseconds,
                Math.Round((double)(outputFileInfo.Length - inputFileInfo.Length) / inputFileInfo.Length * 100, 2));
            
            return outputPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Audio processing: CONVERSION_FAILED - InputFile: {InputPath}, Error: {ErrorMessage}",
                inputPath,
                ex.Message);
            throw new InvalidOperationException($"Audio conversion failed: {ex.Message}", ex);
        }
    }
}
