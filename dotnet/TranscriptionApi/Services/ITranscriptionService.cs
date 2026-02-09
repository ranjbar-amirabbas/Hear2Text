namespace TranscriptionApi.Services;

/// <summary>
/// Service for coordinating audio transcription operations.
/// Manages the transcription workflow including audio processing, worker concurrency, and model invocation.
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Transcribes an audio file in batch mode.
    /// Coordinates audio conversion, worker slot acquisition, and Whisper model transcription.
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file to transcribe</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The transcribed text</returns>
    /// <exception cref="InvalidOperationException">Thrown when transcription fails</exception>
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken ct);
    
    /// <summary>
    /// Transcribes audio data for streaming mode.
    /// Saves audio data to a temporary file, converts it, and transcribes it.
    /// </summary>
    /// <param name="audioData">Binary audio data to transcribe</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The transcribed text</returns>
    /// <exception cref="InvalidOperationException">Thrown when transcription fails</exception>
    Task<string> TranscribeStreamAsync(byte[] audioData, CancellationToken ct);
    
    /// <summary>
    /// Gets a value indicating whether the Whisper model is currently loaded.
    /// </summary>
    bool IsModelLoaded { get; }
    
    /// <summary>
    /// Gets the configured model size (e.g., "tiny", "base", "small", "medium", "large").
    /// </summary>
    string ModelSize { get; }
}
