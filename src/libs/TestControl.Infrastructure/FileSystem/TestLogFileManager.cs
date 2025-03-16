using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace TestControl.Infrastructure.FileSystem;

/// <summary>
/// Handles the logging of test events to a collection of files.
/// </summary>
public partial class TestLogFileManager : IDisposable
{
    [GeneratedRegex(@"([a-z0-9_\-\/\\]+_)(\d{5})\.[a-z]+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex LogFileNameRegex();

    private const string Suffix = "log";
    private const int MaxSizeOfLogFileBytes = 26_214_400; // the threshold for creating a new file.

    private int _fileCounter;
    private FileStream _currentLogFileStream;
    private string _currentLogFilePath;
    private long _currentByteSize;
    private bool _disposedValue;
    private readonly Lock _locker = new();

    private string BuildLogFilePath(string prefix) => $"{prefix}{_fileCounter++:00000}.{Suffix}";

    public TestLogFileManager(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentNullException(nameof(logDirectory));

        var path = Path.GetFullPath(logDirectory);

        LogsDirectory = path;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        StartTime = DateTime.Now;
        UniqueId = Guid.NewGuid().ToString("N")[0..9];
        _currentLogFilePath = BuildLogFilePath(Path.Combine(path, $"{StartTime:yyyyMMdd}_{UniqueId}_"));
        StartLogFile();
    }

    public DateTime StartTime { get; }
    public string UniqueId { get; }
    public string LogsDirectory { get; }

    public void WriteToLog(MessageToControlProgram message)
    {
        if (_currentLogFileStream == null || !_currentLogFileStream.CanWrite)
            return;

        StringBuilder sb = new();
        sb.AppendLine($"[{message.MessageLevel.ToString()}] Source: {message.Source} [{message.Timestamp.ToLocalTime()}]");
        sb.AppendLine(message.Message);
        if (message.Exception != null)
        {
            sb.AppendLine(message.Exception.ToString());
        }
        if (message.ThreadId.HasValue)
        {
            sb.AppendLine($"Thread: {message.ThreadId.Value}");
        }

        lock (_locker)
        {
            _currentLogFileStream.WriteLine(sb.ToString());
            _currentLogFileStream.Flush();
            _currentByteSize = _currentLogFileStream.Length;
        }

        if (_currentByteSize > MaxSizeOfLogFileBytes)
        {
            ChangeLogFiles();
        }
    }

    private void ChangeLogFiles()
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(_currentLogFilePath));

        if (_currentLogFileStream == null)
        {
            StartLogFile();
        }
        else
        {
            lock (_locker)
            {
                var matches = LogFileNameRegex().Matches(_currentLogFilePath);

                Debug.Assert(matches.Count > 0 && matches[0].Groups.Count > 0);

                if (_currentLogFileStream.CanWrite)
                {
                    _currentLogFileStream.Flush();
                    _currentLogFileStream.Close();
                }

                _currentLogFilePath = BuildLogFilePath(matches[0].Groups[1].Value);

                _currentLogFileStream = File.Create(_currentLogFilePath);
                _currentLogFileStream.WriteLine($"File started at local time {DateTime.Now:yyyy-MM-dd HH:mm}");
                _currentLogFileStream.Flush();
                _currentByteSize = _currentLogFileStream.Length;
            }
        }
        Debug.Assert(_currentLogFileStream != null);
    }

    private void StartLogFile()
    {
        lock (_locker)
        {
            _currentLogFileStream = File.Create(_currentLogFilePath);
            _currentLogFileStream.WriteLine($"File started at local time {DateTime.Now:yyyy-MM-dd HH:mm}");
            _currentLogFileStream.Flush();
            _currentByteSize = _currentLogFileStream.Length;
        }
    }

    public void CloseCurrentLogFile()
    {
        lock (_locker)
        {
            if (_currentLogFileStream?.CanWrite ?? false)
            {
                _currentLogFileStream?.Flush();
                _currentLogFileStream?.Close();
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                CloseCurrentLogFile();
                _currentLogFileStream.Dispose();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
