using System.Text;

namespace TestControl.Infrastructure;

public static class StreamExtensions
{
    private static readonly Lock _writeLock = new();

    /// <summary>
    /// Writes a message to the stream if it's writable.
    /// </summary>
    public static void Write(this Stream stream, string message)
    {
        if (stream.CanWrite && !string.IsNullOrEmpty(message))
        {
            ReadOnlySpan<byte> buffer = Encoding.UTF8.GetBytes(message);

            lock (_writeLock)
            {
                stream.Write(buffer);
            }
        }
    }

    /// <summary>
    /// Writes a message asynchronously to the stream if it's writable.
    /// </summary>
    public static async Task WriteAsync(this Stream stream, string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanWrite && !string.IsNullOrEmpty(message) && !cancellationToken.IsCancellationRequested)
        {
            Memory<byte> buffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a message with a newline at the end.
    /// </summary>
    public static void WriteLine(this Stream stream, string message = null)
    {
        stream.Write($"{message ?? string.Empty}{Environment.NewLine}");
    }

    /// <summary>
    /// Writes a message with a newline asynchronously.
    /// </summary>
    public static async Task WriteLineAsync(this Stream stream, string message = null,
        CancellationToken cancellationToken = default)
    {
        await WriteAsync(stream, $"{message ?? string.Empty}{Environment.NewLine}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a timestamped log entry to the stream.
    /// </summary>
    public static void WriteLogEntry(this Stream stream, string message, string logLevel = "INFO")
    {
        string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] {message}";
        stream.WriteLine(timestampedMessage);
    }

    /// <summary>
    /// Writes a timestamped log entry asynchronously.
    /// </summary>
    public static async Task WriteLogEntryAsync(this Stream stream, string message, string logLevel = "INFO",
        CancellationToken cancellationToken = default)
    {
        string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] {message}";
        await WriteLineAsync(stream, timestampedMessage, cancellationToken).ConfigureAwait(false);
    }
}
