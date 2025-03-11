using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices.Workers;

public sealed class Admin
{
    public readonly string Name;
    private readonly HttpClient _httpClient;
    private readonly TestConfig _config;
    private readonly MessageHandler _messageHandler;
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _linkedToken;
    private bool _isActive = false;
    private Collection<Guid> _orgIds = [];
    private Collection<Guid> _workerIds = [];
    private readonly System.Timers.Timer _queryTimer;
    private readonly Lock _timerLock = new();

    public Admin(HttpClient httpClient, TestConfig config, MessageHandler messageHandler, CancellationToken cancellationToken)
    {
        Name = $"{nameof(Admin)}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _messageHandler = messageHandler;
        _cts = new CancellationTokenSource();
        _linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token).Token;

        // Initialize the timer
        _queryTimer = new System.Timers.Timer
        {
            Interval = TimeSpan.FromSeconds(_config.Admins.AdminQueryRoc.InitialFrequencySeconds).TotalMilliseconds,
            AutoReset = true
        };
        _queryTimer.Elapsed += async (sender, e) => await RunQueriesAsync();
    }

    public async Task InitializeAsync()
    {
        var usersToAdd = new Collection<User>();

        var totalTime = TimeSpan.FromSeconds(_config.Admins.AdminGrowthCycleTimeLimitSeconds);
        var startTime = DateTime.Now;

        var self = TestDataCreationService.CreateUser(role: "Admin");

        var parentOrgs = new Organization[_config.Admins.InitialParentOrgsPerAdmin];
        for (int i = 0; i < _config.Admins.InitialParentOrgsPerAdmin; i++)
        {
            parentOrgs[i] = TestDataCreationService.CreateOrg(null);
            _orgIds.Add(parentOrgs[i].OrganizationId);
            usersToAdd.Add(self.CloneForOrgAndRole(parentOrgs[i], "Admin"));
        }

        foreach (var parentOrg in parentOrgs)
        {
            for (int i = 0; i < _config.Admins.InitialOrgsPerParent; i++)
            {
                var childOrg = TestDataCreationService.CreateOrg(parentOrg);
                _orgIds.Add(childOrg.OrganizationId);
                usersToAdd.Add(self.CloneForOrgAndRole(childOrg, "Admin"));

                for (int j = 0; j < _config.Admins.InitialWorkersPerOrg; j++)
                {
                    var worker = TestDataCreationService.CreateUser(childOrg, "Worker");
                    usersToAdd.Add(worker);
                }
            }
        }

        if (usersToAdd.Count > 0)
        {
            var timeLeft = totalTime - (DateTime.Now - startTime);
            int divisor = usersToAdd.Count + 1;

            var apiResponseTimer = Stopwatch.StartNew();
            if (timeLeft > TimeSpan.Zero)
            {
                var timeBuffer = TimeSpan.FromMilliseconds(_config.ResponseThreshold.AverageResponseTimeThresholdMs);
                foreach (var user in usersToAdd)
                {
                    if (_linkedToken.IsCancellationRequested)
                        break;

                    var response = await _httpClient.PostAsJsonAsync("api/user", user, _linkedToken);
                    response.EnsureSuccessStatusCode();

                    if (divisor == 0)
                        continue;

                    timeLeft = totalTime - (DateTime.Now - startTime) - timeBuffer;

                    if (timeLeft <= TimeSpan.Zero)
                    {
                        divisor = 0;
                        continue;
                    }

                    if (divisor > 0)
                    {
                        var delayMs = Convert.ToInt32(Math.Round(
                            ((timeLeft / divisor) - timeBuffer).TotalMilliseconds,
                            0, MidpointRounding.AwayFromZero));

                        if (delayMs > 0)
                        {
                            await Task.Delay(delayMs);
                        }
                    }

                    divisor--;
                }
            }
            else
            {
                // If time has already exceeded, proceed without delays
                foreach (var user in usersToAdd)
                {
                    var response = await _httpClient.PostAsJsonAsync("api/user", user, _linkedToken);
                    response.EnsureSuccessStatusCode();
                }
            }

            /*
             * Stop the test if this work took too long.
             */
            apiResponseTimer.Stop();
            if (apiResponseTimer.Elapsed.TotalSeconds > _config.Admins.AdminGrowthCycleTimeLimitSeconds)
            {
                _messageHandler(new MessageToControlProgram
                {
                    IsTestCancellation = true,
                    Message = $"{nameof(Admin)} '{Name}' failed to complete initialization event within allotted time.",
                    Exception = new Exception($"{nameof(Admin)}.{nameof(InitializeAsync)} took {apiResponseTimer.Elapsed.TotalSeconds} seconds"),
                    MessageLevel = MessageLevel.Critical,
                    Source = Name,
                    ThreadId = Environment.CurrentManagedThreadId
                });
            }
        }

        if (DateTime.Now - startTime > totalTime)
        {
            _cts.Cancel();
            _messageHandler?.Invoke(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = $"{nameof(Admin)} could not complete initialization in allotted time.",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }

        _workerIds = new Collection<Guid>(usersToAdd.Where(u => u.Role == "Worker").Select(x => x.UserId).ToArray());
        _isActive = true;
        _queryTimer.Start();
    }

    public async Task RunQueriesAsync()
    {
        var totalTime = TimeSpan.FromSeconds(_config.Admins.AdminGrowthCycleTimeLimitSeconds);
        var startTime = DateTime.Now;

        if (!_isActive || _linkedToken.IsCancellationRequested || _orgIds.Count == 0)
            return;

        var queries = new[]
        {
            "api/orgs",
            $"api/org/{_orgIds[Random.Shared.Next(_orgIds.Count)]}",
            $"api/users/by-org/{_orgIds[Random.Shared.Next(_orgIds.Count)]}",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Count)]}/transactions",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Count)]}/transactions?status=Pending",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Count)]}/transactions?status=Approved",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Count)]}/transactions?status=Denied",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Count)]}/transactions",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Count)]}/transactions?status=Pending",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Count)]}/transactions?status=Approved",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Count)]}/transactions?status=Denied"
        };

        for (int i = 0; i < _config.Admins.ReportsToRunPerCycle; i++)
        {
            if (_linkedToken.IsCancellationRequested)
                break;

            var query = queries[Random.Shared.Next(queries.Length)];

            try
            {
                var response = await _httpClient.GetAsync(query, _linkedToken);
                response.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
            catch (Exception ex)
            {
                _messageHandler?.Invoke(new MessageToControlProgram
                {
                    Exception = ex,
                    Message = $"Query failed: {query}",
                    MessageLevel = MessageLevel.Error,
                    Source = Name,
                    ThreadId = Environment.CurrentManagedThreadId
                });
            }
        }

        if (DateTime.Now - startTime > totalTime)
        {
            _cts.Cancel();
            _messageHandler?.Invoke(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = $"{nameof(Admin)} could not complete queries in allotted time.",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
    }

    public void DecreaseQueryInterval()
    {
        var currentInterval = _queryTimer.Interval;
        var targetInterval = Math.Max(_config.Admins.AdminQueryRoc.MinFrequencySeconds * 1000,
            currentInterval - _config.Admins.AdminQueryRoc.AmountToDecreaseMs);

        if (targetInterval < currentInterval)
        {
            lock (_timerLock)
            {
                _queryTimer.Stop();
                _queryTimer.Interval = targetInterval;
                _queryTimer.Start();
            }
        }
    }

    public double GetCurrentQueryInterval()
    {
        lock (_timerLock)
        {
            return _queryTimer.Interval; // Return in milliseconds
        }
    }

    /// <summary>
    /// Stops the query timer.
    /// </summary>
    public void Stop()
    {
        _isActive = false;
        _queryTimer.Stop();
    }
}
