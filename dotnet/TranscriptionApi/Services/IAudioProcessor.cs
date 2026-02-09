namespace TranscriptionApi.Services;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Service for processing and validating audio files.
/// Handles file validation, storage, and conversion to Whisper-compatible format.
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    /// Validates whether the uploaded file is a supported audio format.
    /// Checks file extension, size, and MIME type.
    /// </summary>
    /// <param name="file">The uploaded file to validate</param>
    /// <returns>True if the file is valid, false otherwise</returns>
    /// <exception cref="InvalidAudioFormatException">Thrown when file format is not supported</exception>
    /// <exception cref="FileTooLargeException">Thrown when file exceeds size limit</exception>
    bool IsValidAudioFile(IFormFile file);
    
    /// <summary>
    /// Saves an uploaded audio file to a temporary directory.
    /// </summary>
    /// <param name="file">The uploaded file to save</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The full path to the saved file</returns>
    Task<string> SaveUploadedFileAsync(IFormFile file, CancellationToken ct);
    
    /// <summary>
    /// Converts an audio file to Whisper-compatible format (16kHz mono WAV).
    /// Applies audio normalization during conversion.
    /// </summary>
    /// <param name="inputPath">Path to the input audio file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to the converted audio file</returns>
    Task<string> ConvertToWhisperFormatAsync(string inputPath, CancellationToken ct);
}
