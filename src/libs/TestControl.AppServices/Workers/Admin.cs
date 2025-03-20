using System.Collections.ObjectModel;
using System.Net.Http.Json;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices.Workers;

public sealed class Admin : TestWorkerBase
{
    private readonly Collection<Organization> _orgs = [];
    private readonly Collection<Worker> _workers = [];
    private readonly Collection<User> _users = [];

    private Guid[] _orgIds = [];
    private Guid[] _workerIds = [];

    private readonly System.Timers.Timer _queryTimer;
    private readonly Lock _timerLock = new();
    private double _adminGrowthCycleTimeLimitMs;

    public Admin(HttpClient httpClient,
        TestConfig config,
        MessageHandler messageHandler,
        CancellationToken cancellationToken) : base(httpClient, config, messageHandler, cancellationToken)
    {
        _adminGrowthCycleTimeLimitMs = _config.Admins.AdminGrowthCycleTimeLimitSeconds * 1_000D;

        // an orgless self portrait to be used during initialization.
        _self = TestDataCreationService.CreateUser(role: Constants.WorkerTypes.Admin);

        _queryTimer = new System.Timers.Timer
        {
            Interval = TimeSpan.FromSeconds(_config.Admins.AdminQueryRoc.InitialFrequencySeconds).TotalMilliseconds,
            AutoReset = true
        };
        _queryTimer.Elapsed += async (sender, e) => await RunQueriesAsync();
    }

    public async Task InitializeAsync()
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        var totalTime = TimeSpan.FromMilliseconds(_adminGrowthCycleTimeLimitMs);
        var startTime = DateTime.Now;

        await (_mode switch
        {
            Mode.Fair => InitializeForFairness(),
            _ => InitializeForBruteForce()
        });

        await Parallel.ForEachAsync(_workers, async (w, _) => await w.InitializeAsync());

        var curTime = DateTime.Now - startTime;
        if (curTime > totalTime)
        {
            _isActive = false;
            _cts.Cancel();
            _messageHandler?.Invoke(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = $"{Name} could not complete initialization in allotted time ({_adminGrowthCycleTimeLimitMs:F2} ms); time = {curTime.TotalMilliseconds:F2}",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
            return;
        }

        _orgIds = [.. _orgs.Select(x => x.OrganizationId)];
        _workerIds = [.. _workers.Select(x => x.UserId)];

        if (!_linkedToken.IsCancellationRequested)
        {
            _isActive = true;
            var t = RunQueriesAsync();
            _queryTimer.Start();
            await t;
        }
    }

    public async Task RunQueriesAsync()
    {
        if (!_isActive || _linkedToken.IsCancellationRequested || _orgIds.Length == 0 || _workerIds.Length == 0)
            return;

        var totalTime = TimeSpan.FromMilliseconds(_adminGrowthCycleTimeLimitMs);
        var startTime = DateTime.Now;

        var queries = new[]
{
            "api/orgs",
            $"api/org/{_orgIds[Random.Shared.Next(_orgIds.Length)]}",
            $"api/users/by-org/{_orgIds[Random.Shared.Next(_orgIds.Length)]}",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Length)]}/transactions",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Length)]}/transactions?status=Pending",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Length)]}/transactions?status=Approved",
            $"api/organization/{_orgIds[Random.Shared.Next(_orgIds.Length)]}/transactions?status=Denied",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Length)]}/transactions",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Length)]}/transactions?status=Pending",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Length)]}/transactions?status=Approved",
            $"api/user/{_workerIds[Random.Shared.Next(_workerIds.Length)]}/transactions?status=Denied"
        };

        var timeLeft = totalTime - (DateTime.Now - startTime);
        bool timeIsUp = timeLeft <= _config.MinDelay || _mode == Mode.BruteForce;
        var delay = timeIsUp ? _config.MinDelay : timeLeft / (_config.Admins.ReportsToRunPerCycle + 1); // +1 buffer

        for (int i = 0; i < _config.Admins.ReportsToRunPerCycle; i++)
        {
            if (_linkedToken.IsCancellationRequested || !_isActive)
                break;

            var query = queries[Random.Shared.Next(queries.Length)];

            
            _ = await _httpClient.GetAsync(query, _linkedToken);

            if (timeIsUp || delay == _config.MinDelay)
                continue;

            await Task.Delay(delay);
        }

        var curTime = DateTime.Now - startTime;
        if (curTime > totalTime && !_linkedToken.IsCancellationRequested)
        {
            _cts.Cancel();
            _isActive = false;
            _messageHandler?.Invoke(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = $"{Name} could not complete queries in allotted time: {totalTime.TotalMilliseconds:F2} ms; time = {curTime.TotalMilliseconds:F2}",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
    }

    public async Task CompressIntervalsAsync()
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        var t = Task.Run(() => Parallel.ForEachAsync(_workers, async (w, t) => await w.CompressIntervalsAsync()));

        lock (_timerLock)
        {
            var currentInterval = _queryTimer.Interval;
            var targetInterval = Math.Max(_config.Admins.AdminQueryRoc.MinFrequencySeconds * 1_000D,
                currentInterval - _config.Admins.AdminQueryRoc.AmountToDecreaseMs);

            if (targetInterval > 0 && targetInterval < currentInterval)
            {
                _adminGrowthCycleTimeLimitMs = Math.Max(_config.Admins.AdminQueryRoc.MinFrequencySeconds * 1_000D, targetInterval);

                _queryTimer.Stop();
                _queryTimer.Interval = targetInterval;
                _queryTimer.Start();
            }
        }

        await t;
    }

    public void Stop()
    {
        _isActive = false;
        _queryTimer.Stop();
    }

    private async Task InitializeForFairness()
    {
        if (_linkedToken.IsCancellationRequested)
            return;

        var startTime = DateTime.Now;
        var totalTime = TimeSpan.FromMilliseconds(_adminGrowthCycleTimeLimitMs);

        var workUnits = _config.Admins.UnitsOfWorkEstimate;

        HydrateData();

        var timeLeft = totalTime - (DateTime.Now - startTime);
        var delay = (timeLeft / (workUnits * 3));
        delay = delay < _config.MinDelay ? _config.MinDelay : delay;
        bool timeIsUp = delay <= _config.MinDelay;
        foreach (var user in _users)
        {
            if (_linkedToken.IsCancellationRequested)
                break;

            var response = await _httpClient.PostAsJsonAsync("api/user", user, _linkedToken);
            if (timeIsUp)
                continue;

            var x = Task.Delay(delay);

            timeLeft = totalTime - (DateTime.Now - startTime);
            timeIsUp = timeLeft <= _config.MinDelay;

            await x;
        }
    }

    private async Task InitializeForBruteForce()
    {
        _linkedToken.ThrowIfCancellationRequested();

        HydrateData();

        if (_users.Count > 0)
        {
            await Parallel.ForEachAsync(_users, _linkedToken, async (u, token) =>
            {
                await _httpClient.PostAsJsonAsync("api/user", u, token);
                await Task.Delay(_config.MinDelayMs, token);
            });
        }
    }

    private void HydrateData()
    {
        for (int i = 0; i < _config.Admins.InitialParentOrgsPerAdmin; i++)
        {
            var org = TestDataCreationService.CreateOrg(null);
            var user = _self.CloneForOrgAndRole(org, Constants.WorkerTypes.Admin);

            _orgs.Add(org);
            _users.Add(user);
        }

        // they are all parent orgs at this point.
        var parentOrgs = _orgs.ToArray();

        foreach (var parentOrg in parentOrgs)
        {
            for (int i = 0; i < _config.Admins.InitialOrgsPerParent; i++)
            {
                var childOrg = TestDataCreationService.CreateOrg(parentOrg);
                _orgs.Add(childOrg);
                _users.Add(_self.CloneForOrgAndRole(childOrg, Constants.WorkerTypes.Admin));

                for (int j = 0; j < _config.Admins.InitialWorkersPerOrg; j++)
                {
                    var workerUser = TestDataCreationService.CreateUser(childOrg, Constants.WorkerTypes.Worker);
                    _users.Add(workerUser);
                    var worker = new Worker(_httpClient, _config, _messageHandler, workerUser, _linkedToken);
                    _workers.Add(worker);
                }
            }
        }
    }
}
