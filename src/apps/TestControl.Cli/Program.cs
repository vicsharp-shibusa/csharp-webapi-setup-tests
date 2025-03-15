using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.FileSystem;
using TestControl.Infrastructure.SubjectApiPublic;

class Program
{
    const string SourceName = "Control";
    const int DefaultExitCode = -1;
    static int? _exitCode = null;
    static bool _verbose;
    static bool _saveLogs;
    static CancellationTokenSource _cts;
    static TestConfig _config;
    static TestRunner _testRunner;
    static TestLogFileManager _logFileManager;
    static readonly Lock _locker = new();

    static async Task Main(string[] args)
    {
        _cts = new CancellationTokenSource();

        Console.CancelKeyPress += HandleShutdown;

        var executionTimer = Stopwatch.StartNew();

        try
        {
            await SetupTestAsync(args);

            /*
             * Using a separate HttpClient here so as not to skew the average response time results
             * (the httpClient that comes from `CreateHttpClientForTests` below
             * has a special handler for recording response times).
             */
            using (var initHttpClient = new HttpClient() { BaseAddress = new Uri(_config.Api.ApiBaseUrl) })
            {
                await InitializeTestEnvironment(initHttpClient);
                await WarmupTestEnvironment(initHttpClient);
            }

            _testRunner = new TestRunner(_config, MessageHandler, _cts.Token);

            await _testRunner.RunAsync();

            _exitCode = 0;
        }
        catch (TaskCanceledException)
        {
            _exitCode ??= 1;
            Communicate("Task cancelled");
        }
        catch (OperationCanceledException)
        {
            _exitCode ??= 2;
            Communicate("Test was cancelled.");
        }
        catch (AggregateException ex)
        {
            _exitCode ??= 3;
            Communicate(ex);
            foreach (var e in ex.InnerExceptions)
            {
                Communicate(e);
            }
        }
        catch (Exception ex)
        {
            _exitCode ??= 4;
            Communicate(ex);
        }
        finally
        {
            Communicate($"[{DateTime.Now:HH:mm:ss}] Shutting down.");
            TestStatus status = null;

            if (_testRunner != null)
            {
                using var httpClient = new HttpClient { BaseAddress = new Uri(_config.Api.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
                try
                {
                    status = await _testRunner.GetStatusAsync(httpClient);
                }
                catch (TaskCanceledException)
                {
                    Communicate($"[{DateTime.Now:HH:mm:ss}]]Status fetch timed out.");
                }
            }

            if (status != null)
            {
                var json = JsonSerializer.Serialize(new FinalReport()
                {
                    Config = _config,
                    Status = status
                }, new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var path = Path.Combine(_logFileManager.LogsDirectory,
                    $"{_logFileManager.StartTime:yyyyMMdd}_{_logFileManager.UniqueId}_final.json");

                var t = File.WriteAllTextAsync(path, json);
                Communicate(status.ToString());
                Communicate("Writing final JSON file.");
                await t;
            }

            executionTimer.Stop();
            Communicate($"[{DateTime.Now:HH:mm:ss}] Program completed in {executionTimer.Elapsed.TotalMinutes:#,##0.00} minutes with exit code {_exitCode}");
            _logFileManager?.CloseCurrentLogFile();
            _logFileManager?.Dispose();
            _testRunner?.Dispose();
            _cts.Dispose();
#if DEBUG
            Console.WriteLine("Hit ENTER to finish.");
            Console.ReadLine();
#endif
            Environment.Exit(_exitCode ?? DefaultExitCode);
        }
    }

    private record FinalReport
    {
        public TestConfig Config { get; init; }
        public TestStatus Status { get; init; }
    }

    private static async Task<ApiInfo> CheckApiStatusAsync()
    {
        ApiInfo results = new(false, null, null, null);

        try
        {
            using var httpClient = new HttpClient() { BaseAddress = new Uri(_config.Api.ApiBaseUrl) };

            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                Constants.TestUris.Name));

            var status = await response.Content.ReadFromJsonAsync<ApiInfo>(JsonSerializerOptions.Web);

            results = new(response.IsSuccessStatusCode, status.ServerName, status.DbVersion, response.StatusCode.ToString());
        }
        catch (Exception exc)
        {
            results = new(false, null, null, exc.Message);
        }

        return results;
    }

    private static async Task SetupTestAsync(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-c":
                case "--config":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException($"Expecting a file path after {args[i]}");

                    var configFilePath = args[++i];

                    if (!File.Exists(configFilePath))
                        throw new ArgumentException($"Config file not found: {configFilePath}");

                    try
                    {
                        _config = TestConfig.LoadFromFile(configFilePath);
                    }
                    catch (Exception exc)
                    {
                        throw new ArgumentException($"Could not load file '{configFilePath}'", exc);
                    }

                    break;
                case "-l":
                case "--logs-directory":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException($"Expecting a directory path after {args[i]}");
                    _logFileManager = new TestLogFileManager(args[++i]);
                    _saveLogs = true; // if a log directory is provided, assume the user wants to save logs.
                    break;
                case "-v":
                case "--verbose":
                    _verbose = true;
                    break;
                case "--save-logs":
                    _saveLogs = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        if (_config == null)
            throw new ArgumentException($"Expected a config file to be provided with the -c <file name> argument.");

        /*
         * Set up a default location for logs.
         * This is not recommended as publishing a new version of the appl will overwrite your logs.
         * Better to provide a separate log location via the `-l <directory>` argument.
         */
        if (_logFileManager == null && _saveLogs)
        {
            _logFileManager = new TestLogFileManager(Path.Combine("logs", _config.Api.ApiName,
                DateTime.Now.ToString("yyyyMMddHHmm")));
        }
        Debug.Assert(!_saveLogs || (_saveLogs && _logFileManager != null));

        /*
         * Validate the test configuration file provided.
         */
        StringBuilder badConfigSb = new();
        foreach (var msg in _config.Api.GetValidationMessages())
        {
            badConfigSb.AppendLine($"\t{msg}");
        }

        if (badConfigSb.Length > 0)
        {
            const string Divider = "##############\r\n";
            badConfigSb.Insert(0, $"Configuration file failed validation.{Environment.NewLine}{Divider}");
            badConfigSb.AppendLine(Divider);
            Communicate(badConfigSb.ToString());
            throw new InvalidOperationException("Configuration file failed validation checks.");
        }

        var apiStatus = await CheckApiStatusAsync();

        if (!apiStatus.IsRunning)
        {
            throw new Exception($"API '{_config.Api.ApiName}' is not running.");
        }

        if (!apiStatus.VersionsMatch(_config.Api.ApiName, _config.Api.DatabaseVersion))
        {
            StringBuilder sb = new("API name does not match config file value.");
            sb.Append($" Names: Local='{_config.Api.ApiName}'; Server='{apiStatus.ServerName}'");
            sb.Append($" DB Versions: Local: '{_config.Api.DatabaseVersion}'; Server: '{apiStatus.DbVersion}'");
            throw new ArgumentException(sb.ToString());
        }

        Communicate("Starting TestControl CLI...");
        Communicate($"Target API: {_config.Api.ApiBaseUrl}");
    }

    private static string GetCompletionMessage(Stopwatch timer) => $"Completed in {timer.Elapsed.TotalMilliseconds:F2} ms.";

    private static async Task InitializeTestEnvironment(HttpClient httpClient)
    {
        var timer = Stopwatch.StartNew();
        Communicate("Initializing test environment... Purging database.");

        var response = await httpClient.PostAsync(Constants.TestUris.TestInitialize, null);

        timer.Stop();
        string msg = GetCompletionMessage(timer);

        if (response.IsSuccessStatusCode)
        {
            Communicate($"Database purged successfully. {msg}");
        }
        else
        {
            Communicate(new Exception($"Failed to purge database. Status: {response.StatusCode}. {msg}"));
        }
    }

    private static async Task WarmupTestEnvironment(HttpClient httpClient)
    {
        var timer = Stopwatch.StartNew();
        Communicate($"Warming up API with {_config.WarmupCycles} cycles...");

        var response = await httpClient.PostAsJsonAsync(Constants.TestUris.TestWarmup, _config);
        timer.Stop();
        string msg = GetCompletionMessage(timer);

        if (response.IsSuccessStatusCode)
        {
            Communicate($"API warmed up successfully. {msg}");
        }
        else
        {
            Communicate(new Exception($"Failed to warm up API. Status: {response.StatusCode}. {msg}"));
        }
    }

    private static void Communicate(string message) => MessageHandler(new MessageToControlProgram()
    {
        Message = message,
        MessageLevel = MessageLevel.Info,
        Source = SourceName,
        ThreadId = Environment.CurrentManagedThreadId
    });

    private static void Communicate(Exception exc) => MessageHandler(new MessageToControlProgram()
    {
        Exception = exc,
        Message = exc.Message,
        MessageLevel = MessageLevel.Error,
        Source = SourceName,
        ThreadId = Environment.CurrentManagedThreadId
    });

    private static void MessageHandler(MessageToControlProgram message)
    {
        if (message.Exception != null)
        {
            Console.WriteLine("[Error] A critical exception occurred.");
            Console.WriteLine(message.Message ?? message.Exception.Message);
#if DEBUG
            Console.WriteLine(message.Exception.ToString());
#endif
            _cts.Cancel();
        }
        else if (_verbose)
        {
            lock (_locker)
            {
                Console.ForegroundColor = message.MessageLevel switch
                {
                    MessageLevel.Info => ConsoleColor.Gray,
                    MessageLevel.Warning => ConsoleColor.Yellow,
                    MessageLevel.Critical or MessageLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"[{message.Timestamp.ToLocalTime():HH:mm:ss}][{message.MessageLevel,7}] src:{message.Source}::{message.ThreadId.GetValueOrDefault(),3} : {message.Message}");
                Console.ResetColor();
            }
        }

        if (_saveLogs)
        {
            _logFileManager.WriteToLog(message);
        }

        if (message.IsTestCancellation && !_cts.IsCancellationRequested)
            _cts.Cancel();
    }

    private static void HandleShutdown(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true; // Prevents immediate termination
        Communicate("CTRL+C detected. Shutting down gracefully...");
        _cts.Cancel(); // Signal cancellation

        Task.Run(async () =>
        {
            await Task.Delay(Math.Max(0, _config.ShutdownDelayMs));
            ShutdownCleanup();
            Environment.Exit(0);
        });
    }

    private static void ShutdownCleanup()
    {
        _logFileManager?.CloseCurrentLogFile();
        Communicate("Shutdown complete.");
    }
}
