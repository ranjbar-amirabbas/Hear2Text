namespace TranscriptionApi.Exceptions;

/// <summary>
/// Exception thrown when an uploaded audio file has an unsupported format.
/// Maps to HTTP 415 Unsupported Media Type.
/// </summary>
public class InvalidAudioFormatException : Exception
{
    public InvalidAudioFormatException()
        : base("Unsupported audio format. Supported formats: WAV, MP3, OGG, M4A")
    {
    }

    public InvalidAudioFormatException(string message)
        : base(message)
    {
    }

    public InvalidAudioFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exception with details about the invalid file extension.
    /// </summary>
    /// <param name="fileExtension">The invalid file extension that was provided</param>
    public static InvalidAudioFormatException ForExtension(string fileExtension)
    {
        return new InvalidAudioFormatException(
            $"Unsupported audio format. Supported formats: WAV, MP3, OGG, M4A. Provided: {fileExtension}");
    }
}
