﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Timers;
using TestControl.AppServices.Workers;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices;

public sealed class TestRunner : IDisposable
{
    private bool _disposedValue;

    private readonly TestConfig _config;
    private readonly Collection<Admin> _admins = [];

    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _linkedToken;

    private readonly ResponseTimeHandler _responseTimeHandler;
    private readonly HttpClient _httpClient;

    private readonly MessageHandler _messageHandler;

    private readonly System.Timers.Timer _statusTimer;

    public TestRunner(TestConfig config, MessageHandler messageHandler, CancellationToken cancellationToken)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));

        _cts = new CancellationTokenSource();
        _linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token).Token;

        _responseTimeHandler = CreateResponseTimeHandler();

        _httpClient = new HttpClient(_responseTimeHandler) { BaseAddress = new Uri(_config.Api.ApiBaseUrl) };

        _statusTimer = new(_config.StatusCheckIntervalSeconds * 1_000D) { AutoReset = true };
        _statusTimer.Elapsed += StatusCycleCallback;
    }

    public string Status { get; private set; } = "Waiting";

    public async Task RunAsync()
    {
        Status = "Running";
        Communicate("Test starting");

        if (_config.TestDurationMinutes > 0)
            _cts.CancelAfter(TimeSpan.FromMinutes(_config.TestDurationMinutes));

        _statusTimer.Start();

        /*
         * First we grow the admins at a steady pace until we reach our limit.
         */
        Status = "Phase 1; Admin growth";
        await GrowAdminsToLimit(_config.Admins.MaxAdmins);

        /*
         * Then we start compressing the time windows.
         */
        try
        {
            Status = "Phase 2: cycle compression";
            System.Timers.Timer adminRocQueryTimer = new(_config.Admins.AdminQueryRoc.FrequencyToDecreaseIntervalSeconds * 1_000D) { AutoReset = true };
            adminRocQueryTimer.Elapsed += (sender, e) => AdminRocQueryTimerCallback(sender, e);
            adminRocQueryTimer.Start();

            // Wait indefinitely until cancelled (or use TestDurationMinutes if preferred)
            await Task.Delay(Timeout.Infinite, _linkedToken);
            adminRocQueryTimer.Stop();
            adminRocQueryTimer.Dispose();
            Status = "Completed";
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
            _messageHandler(new MessageToControlProgram
            {
                IsTestCancellation = true,
                Message = "Test cancelled.",
                MessageLevel = MessageLevel.Error,
                Source = nameof(TestRunner),
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
        finally
        {
            Stop();
        }
    }

    private async void AdminRocQueryTimerCallback(object sender, ElapsedEventArgs e)
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        Communicate("Decreasing query interval and time allocation for admins.");

        await Parallel.ForEachAsync(_admins, async (admin, _) => await admin.CompressIntervalsAsync());
    }

    /// <summary>
    /// Grows the number of admins to the specified limit with pacing.
    /// </summary>
    /// <param name="limit">Maximum number of admins to create.</param>
    private async Task GrowAdminsToLimit(int limit)
    {
        _messageHandler(new MessageToControlProgram
        {
            Message = $"Growing admins to limit of {limit:#,##0}...",
            MessageLevel = MessageLevel.Info,
            Source = nameof(TestRunner),
            ThreadId = Environment.CurrentManagedThreadId
        });

        if (_admins.Count == 0)
        {
            for (int a = 0; a < _config.Admins.InitialAdmins; a++)
            {
                var admin = new Admin(_httpClient, _config, _messageHandler, _linkedToken);
                await admin.InitializeAsync();
                _admins.Add(admin);
                if (_admins.Count >= _config.Admins.MaxAdmins)
                    break;
            }
        }

        while (_admins.Count < limit && !_linkedToken.IsCancellationRequested)
        {
            double cycleTimeMs = _config.Admins.AdminGrowthCycleFrequencyMs;

            // Start measuring the time for admin creation
            for (int a = 0; a < _admins.Count; a++)
            {
                if (_linkedToken.IsCancellationRequested || _admins.Count >= limit)
                    break;

                for (int g = 0; g < _config.Admins.AdminGrowthPerAdmin; g++)
                {
                    if (_linkedToken.IsCancellationRequested)
                        break;

                    var stopwatch = Stopwatch.StartNew();

                    var admin = new Admin(_httpClient, _config, _messageHandler, _linkedToken);
                    await admin.InitializeAsync();
                    stopwatch.Stop();

                    double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                    // Check if admin creation exceeded the allotted cycle time
                    if (elapsedMs > cycleTimeMs)
                    {
                        _messageHandler(new MessageToControlProgram
                        {
                            IsTestCancellation = true,
                            Message = $"Admin '{admin.Name}' creation took {elapsedMs:F2} ms, exceeding cycle time of {cycleTimeMs:F2} ms.",
                            MessageLevel = MessageLevel.Critical,
                            Source = nameof(TestRunner),
                            ThreadId = Environment.CurrentManagedThreadId
                        });
                        return;
                    }

                    _admins.Add(admin);
                    if (_admins.Count >= limit)
                        break;
                }
            }
        }
    }

    private bool _isStopping = false;

    /// <summary>
    /// Stops the test and all admin activities.
    /// </summary>
    public void Stop()
    {
        if (!_isStopping)
        {
            _isStopping = true;
            _cts.Cancel();

            _statusTimer.Stop();

            Parallel.ForEach(_admins, a => a.Stop());
        }
    }

    private void HandleFailure(string reason)
    {
        Status = $"Failure: {reason}";
        _messageHandler(new MessageToControlProgram
        {
            Message = $"Test failed: {reason}",
            MessageLevel = MessageLevel.Critical,
            Source = nameof(TestRunner),
            ThreadId = Environment.CurrentManagedThreadId
        });
        _cts.Cancel();
    }

    private void Communicate(string message) => _messageHandler(new MessageToControlProgram()
    {
        Message = message,
        MessageLevel = MessageLevel.Info,
        Source = nameof(TestRunner),
        ThreadId = Environment.CurrentManagedThreadId
    });


    private async void QueryCycleCallback(object sender, ElapsedEventArgs e)
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        try
        {
            foreach (var admin in _admins)
            {
                await admin.RunQueriesAsync();
            }
        }
        catch (Exception ex)
        {
            HandleFailure($"Exception during {nameof(QueryCycleCallback)}: {ex.Message}");
        }
    }

    private async void StatusCycleCallback(object sender, ElapsedEventArgs e)
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        try
        {
            Communicate((await GetStatusAsync()).ToString());
        }
        catch (Exception ex)
        {
            HandleFailure($"Exception during {nameof(StatusCycleCallback)}: {ex.Message}");
        }
    }

    public async Task<TestStatus> GetStatusAsync(HttpClient httpClient = null)
    {
        httpClient ??= _httpClient;

        try
        {
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{Constants.TestUris.Status}?responseTimeThreshold={_config.ResponseThreshold.AverageResponseTimeThresholdMs}"));
            var status = await response.Content.ReadFromJsonAsync<TestStatus>(JsonSerializerOptions.Web);
            status.MovingAvgResponseTime = _responseTimeHandler.CurrentResponseAverageMs;
            status.ResponseTimeThreshold = _config.ResponseThreshold.AverageResponseTimeThresholdMs;
            status.Status = Status;
            return status;
        }
        catch (TaskCanceledException)
        {
            // Log timeout and return null or a default status
            Console.WriteLine($"Status fetch timed out at {DateTime.Now:HH:mm:ss}");
            return new TestStatus { /* Default values */ };
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Stop();
                _statusTimer.Dispose();
                _cts.Dispose();
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

    private ResponseTimeHandler CreateResponseTimeHandler()
    {
        /* This "DelegatingHandler" (ResponseTimeHandler) addresses response time threshold violations
         * and API failure status codes.
         */
        var innerHandler = new HttpClientHandler();
        var responseTimeHandler = new ResponseTimeHandler(_config.ResponseThreshold.AverageResponseTimePeriod,
            _config.ResponseThreshold.AverageResponseTimeThresholdMs, _cts)
        {
            InnerHandler = innerHandler
        };

        /*
         * ResponseTimeHandler has two events - one for processed responses
         * and one for the cancellation event, which means the response time threshold was reached.
         */
        responseTimeHandler.OnResponseProcessed += (sender, args) =>
        {
            Communicate(args.Message);
        };

        responseTimeHandler.OnCancellation += (sender, args) =>
        {
            _messageHandler(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = args.Message,
                MessageLevel = MessageLevel.Critical,
                Source = nameof(ResponseTimeHandler),
                ThreadId = Environment.CurrentManagedThreadId
            });
        };

        return responseTimeHandler;
    }
}
