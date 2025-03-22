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

public static class TimeSpanExtensions
{
    public static string ToUserSpeak(this TimeSpan timeSpan)
    {
        // Handle negative TimeSpans
        if (timeSpan.TotalMilliseconds < 0)
        {
            return "negative " + ToUserSpeak(timeSpan.Negate());
        }

        // Handle zero
        if (timeSpan.TotalMilliseconds == 0)
        {
            return "0 seconds";
        }

        // Break down the TimeSpan into components
        int days = timeSpan.Days;
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;
        int seconds = timeSpan.Seconds;
        int milliseconds = timeSpan.Milliseconds;

        var parts = new List<string>();

        // Add days if present
        if (days > 0)
        {
            parts.Add($"{days} day{(days == 1 ? "" : "s")}");
        }

        // Add hours if present
        if (hours > 0)
        {
            parts.Add($"{hours} hour{(hours == 1 ? "" : "s")}");
        }

        // Add minutes if present
        if (minutes > 0)
        {
            parts.Add($"{minutes} minute{(minutes == 1 ? "" : "s")}");
        }

        // Add seconds if present
        if (seconds > 0)
        {
            parts.Add($"{seconds} second{(seconds == 1 ? "" : "s")}");
        }

        // Add milliseconds if present and no larger units
        if (milliseconds > 0 && days == 0 && hours == 0 && minutes == 0 && seconds == 0)
        {
            parts.Add($"{milliseconds} millisecond{(milliseconds == 1 ? "" : "s")}");
        }

        // Combine parts with proper grammar
        return parts.Count switch
        {
            0 => "0 seconds",
            1 => parts[0],
            2 => $"{parts[0]} and {parts[1]}",
            _ => string.Join(", ", parts.Take(parts.Count - 1)) + $" and {parts.Last()}",
        };
    }
}