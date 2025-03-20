using System.Diagnostics;

namespace TestControl.AppServices;

public sealed class ResponseTimeHandler : DelegatingHandler
{
    private ulong _totalMilliseconds;
    private readonly Queue<double> _responseTimes;
    private int _numberCalls;          // Total number of calls made by this handler
    private readonly int _maxPeriods;  // Number of values in the moving average
    private readonly int _thresholdMs; // Threshold for cancellation
    private readonly CancellationTokenSource _cts;
    private readonly Lock _lock = new();

    public event EventHandler<ResponseTimeEventArgs> OnResponseProcessed;
    public event EventHandler<ResponseTimeEventArgs> OnCancellation;

    public ResponseTimeHandler(int maxPeriods, int thresholdMs, CancellationTokenSource cts)
    {
        _responseTimes = new(maxPeriods);
        _maxPeriods = maxPeriods;
        _thresholdMs = thresholdMs;
        _cts = cts;
    }

    public double CurrentResponseAverageMs => _responseTimes.Average();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Interlocked.Increment(ref _numberCalls);

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        double responseTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        double ma;
        int count;
        lock (_lock)
        {
            _totalMilliseconds += Convert.ToUInt64(responseTimeMs);

            if (_responseTimes.Count == _maxPeriods)
            {
                _ = _responseTimes.Dequeue();
            }
            _responseTimes.Enqueue(responseTimeMs);
            ma = _responseTimes.Average();
            count = _responseTimes.Count;
        }

        var ts = DateTimeOffset.Now;
        var args = new ResponseTimeEventArgs
        {
            ResponseTimeMs = responseTimeMs,
            MovingAverageMs = ma,
            NumberCalls = _numberCalls,
            TotalResponseTime = TimeSpan.FromMicroseconds(_totalMilliseconds),
            Timestamp = ts,
            Message = $"[{DateTime.Now:HH:mm:ss}] [{responseTimeMs,8:F2}] [{ma,8:F2}] '{request.RequestUri?.AbsoluteUri}'"
        };

        if (!response.IsSuccessStatusCode) // fail if api fails
        {
            args.ThresholdExceeded = false;
            args.Message = $"[{DateTime.Now:HH:mm:ss}] API failure code ({response.StatusCode}) for call '{request.RequestUri?.AbsoluteUri}' at {ts:HH:mm:ss}";
            OnCancellation?.Invoke(this, args);
            _cts.Cancel();
        }
        else if (ma > _thresholdMs && count == _maxPeriods) // fail if threshold reached
        {
            args.ThresholdExceeded = true;
            args.Message = $"Threshold exceeded with value of {ma:#,##0.00} ms at {ts:HH:mm:ss} for request {request.Method.Method} {request.RequestUri?.AbsoluteUri}";
            OnCancellation?.Invoke(this, args);
            _cts.Cancel();
        }
        else
        {
            OnResponseProcessed?.Invoke(this, args);
        }

        return response;
    }
}

public class ResponseTimeEventArgs : EventArgs
{
    public TimeSpan TotalResponseTime { get; init; }
    public double ResponseTimeMs { get; init; }
    public bool ThresholdExceeded { get; set; }
    public double MovingAverageMs { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public int NumberCalls { get; init; }
    public string Message { get; set; }
}
