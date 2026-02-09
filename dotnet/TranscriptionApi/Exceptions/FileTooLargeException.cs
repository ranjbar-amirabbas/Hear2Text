namespace TranscriptionApi.Exceptions;

/// <summary>
/// Exception thrown when an uploaded audio file exceeds the maximum allowed size.
/// Maps to HTTP 413 Payload Too Large.
/// </summary>
public class FileTooLargeException : Exception
{
    public long FileSize { get; }
    public long MaxSize { get; }

    public FileTooLargeException(long fileSize, long maxSize)
        : base($"File size ({FormatBytes(fileSize)}) exceeds maximum allowed size ({FormatBytes(maxSize)})")
    {
        FileSize = fileSize;
        MaxSize = maxSize;
    }

    public FileTooLargeException(string message)
        : base(message)
    {
    }

    public FileTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
