namespace TestControl.Infrastructure;

/// <summary>
/// Represents a message to be sent to the test control.
/// </summary>
public readonly record struct MessageToControlProgram
{
    public MessageToControlProgram()
    {
    }

    public MessageToControlProgram(Exception exc, string message = null)
    {
        Exception = exc;
        Message = message ?? exc.Message;
        MessageLevel = MessageLevel.Error;
        ThreadId = Thread.CurrentThread?.ManagedThreadId;
    }

    /// <summary>
    /// Gets an indicator if the test should be cancelled.
    /// </summary>
    public bool IsTestCancellation { get; init; }
    /// <summary>
    /// Gets a message to the control program.
    /// </summary>
    public string Message { get; init; }
    /// <summary>
    /// Gets the source of the message.
    /// </summary>
    public string Source { get; init; }
    /// <summary>
    /// Gets the <see cref="MessageLevel"/>.
    /// </summary>
    public MessageLevel MessageLevel { get; init; } 
    /// <summary>
    /// Gets the thread id from which the message was sent.
    /// </summary>
    public int? ThreadId { get; init; } = null;
    /// <summary>
    /// Gets the exception thrown, if any.
    /// </summary>
    public Exception Exception { get; init; } = null;
    /// <summary>
    /// Gets a <see cref="DateTimeOffset"/> for the creation of the message.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTime.Now;
}
