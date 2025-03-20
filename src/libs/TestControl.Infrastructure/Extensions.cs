using System.Text;

namespace TestControl.Infrastructure;

/// <summary>
/// Represents a set of <see cref="Stream"/> extensions to simplify writing text to the stream.
/// </summary>
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

        if (stream.CanWrite && !string.IsNullOrEmpty(message))
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
        Write(stream, $"{message ?? string.Empty}{Environment.NewLine}");
    }

    /// <summary>
    /// Writes a message with a newline asynchronously.
    /// </summary>
    public static async Task WriteLineAsync(this Stream stream, string message = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await WriteAsync(stream, $"{message ?? string.Empty}{Environment.NewLine}", cancellationToken)
            .ConfigureAwait(false);
    }
}
