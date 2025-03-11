namespace TestControl.Infrastructure;

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

    public bool IsTestCancellation { get; init; }
    public string Message { get; init; }
    public string Source { get; init; }
    public MessageLevel MessageLevel { get; init; } 
    public int? ThreadId { get; init; } = null;
    public Exception Exception { get; init; } = null;
    public DateTimeOffset Timestamp { get; } = DateTime.Now;
}
