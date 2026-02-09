namespace TranscriptionApi.Services;

/// <summary>
/// Service for managing and using the Whisper speech recognition model.
/// Handles model loading, lifecycle management, and transcription operations.
/// </summary>
public interface IWhisperModelService
{
    /// <summary>
    /// Transcribes an audio file using the Whisper model.
    /// The model must be loaded before calling this method.
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file to transcribe (must be in Whisper format: 16kHz mono WAV)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The transcribed text</returns>
    /// <exception cref="InvalidOperationException">Thrown when model is not loaded</exception>
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken ct);
    
    /// <summary>
    /// Loads the Whisper model into memory.
    /// This operation is thread-safe and will only load the model once.
    /// Subsequent calls will return immediately if the model is already loaded.
    /// </summary>
    /// <returns>A task representing the asynchronous load operation</returns>
    Task LoadModelAsync();
    
    /// <summary>
    /// Gets a value indicating whether the Whisper model is currently loaded.
    /// </summary>
    bool IsLoaded { get; }
    
    /// <summary>
    /// Gets the configured model size (e.g., "tiny", "base", "small", "medium", "large").
    /// </summary>
    string ModelSize { get; }
}
