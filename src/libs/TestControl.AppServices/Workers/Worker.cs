using System.Net.Http.Json;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices.Workers;

public sealed class Worker : TestWorkerBase
{
    private readonly System.Timers.Timer _transactionTimer;
    private double _transactionCycleTimeLimitMs;
    private readonly Lock _timerLock = new();

    public Worker(HttpClient httpClient, TestConfig config,
        MessageHandler messageHandler, User user, CancellationToken cancellationToken) :
        base(httpClient, config, messageHandler, cancellationToken)
    {
        _self = user;
        _transactionCycleTimeLimitMs = _config.Workers.WorkerCycleTimeLimitSeconds * 1_000D;

        _transactionTimer = new System.Timers.Timer
        {
            Interval = _config.Workers.WorkerTransactionsRoc.InitialFrequencySeconds * 1_000D,
            AutoReset = true
        };
        _transactionTimer.Elapsed += async (sender, e) => await RunTransactionsAsync();
    }

    public Task InitializeAsync()
    {
        _linkedToken.ThrowIfCancellationRequested();

        _isActive = true;
        _transactionTimer.Start();

        return (_transactionTimer.Interval > 50) ? RunTransactionsAsync() : Task.CompletedTask;
    }

    public async Task RunTransactionsAsync()
    {
        _linkedToken.ThrowIfCancellationRequested();

        if (!_isActive)
            return;

        var transactionsToAdd = new UserTransaction[_config.Workers.TransactionsToCreatePerCycle];

        var startTime = DateTime.Now;
        var totalTime = TimeSpan.FromMilliseconds(_transactionCycleTimeLimitMs);

        for (int i = 0; i < _config.Workers.TransactionsToCreatePerCycle; i++)
        {
            transactionsToAdd[i] = TestDataCreationService.CreateTransaction(_self, UserTransactionType.Pending.ToString());
        }

        var timeLeft = (totalTime - (DateTime.Now - startTime)) / 3D; // cut by a third because part 2 (see below) is the heavier workload.
        bool timeIsUp = timeLeft <= _config.MinDelay || _mode == Mode.BruteForce;
        var delay = timeIsUp ? _config.MinDelay : timeLeft / (_config.Workers.TransactionsToCreatePerCycle + 1); // +1 buffer

        foreach (var transaction in transactionsToAdd)
        {
            _linkedToken.ThrowIfCancellationRequested();

            var response = await _httpClient.PostAsJsonAsync("api/transaction", transaction, _linkedToken);

            if (timeIsUp)
                continue;

            await Task.Delay(delay);
        }

        _linkedToken.ThrowIfCancellationRequested();

        var pendingTransactions = (await _httpClient.GetFromJsonAsync<IEnumerable<UserTransaction>>(
            $"api/organization/{_self.Organization.OrganizationId}/transactions?status=Pending", _linkedToken))
            .Where(x => !x.User.UserId.Equals(_self.UserId)).ToArray();

        if (pendingTransactions.Length > 0)
        {
            timeLeft = (totalTime - (DateTime.Now - startTime));
            timeIsUp = timeLeft <= _config.MinDelay || _mode == Mode.BruteForce;
            delay = timeIsUp ? _config.MinDelay : timeLeft / (_config.Workers.TransactionsToEvaluatePerCycle + 1);

            var counter = 0;
            List<Task> tasks = [];
            foreach (var pt in pendingTransactions)
            {
                _linkedToken.ThrowIfCancellationRequested();
                if (!_isActive || counter == _config.Workers.TransactionsToEvaluatePerCycle)
                    break;

                pt.Status = Random.Shared.Next(2) == 0 ? UserTransactionType.Approved.ToString()
                    : UserTransactionType.Denied.ToString();

                tasks.Add(_httpClient.PutAsJsonAsync<UserTransaction>("api/transaction", pt, _linkedToken));
                counter++;

                if (counter == _config.Workers.TransactionsToEvaluatePerCycle)
                    break;

                if (timeIsUp)
                    continue;

                await Task.Delay(delay);
            }

            await Task.WhenAll(tasks);
        }

        var curTime = DateTime.Now - startTime;
        if (curTime > totalTime)
        {
            _cts.Cancel();
            _isActive = false;
            _messageHandler?.Invoke(new MessageToControlProgram()
            {
                IsTestCancellation = true,
                Message = $"{Name} could not complete initialization in allotted time ({totalTime.TotalMilliseconds:F2}): time = {curTime.TotalMilliseconds:F2}",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
    }

    public async Task CompressIntervalsAsync()
    {
        if (_linkedToken.IsCancellationRequested || !_isActive)
            return;

        var currentInterval = _transactionTimer.Interval;
        var targetInterval = Math.Max(_config.Workers.WorkerTransactionsRoc.MinFrequencySeconds * 1_000D,
            currentInterval - _config.Workers.WorkerTransactionsRoc.AmountToDecreaseMs);

        if (targetInterval > 0 && targetInterval < currentInterval)
        {
            lock (_timerLock)
            {
                _transactionCycleTimeLimitMs = Math.Max(_config.Workers.WorkerTransactionsRoc.MinFrequencySeconds * 1_000D, targetInterval);

                _transactionTimer.Stop();
                _transactionTimer.Interval = targetInterval;
                _transactionTimer.Start();
            }
            await RunTransactionsAsync(); // don't wait for the first tick on the new timer.
        }
    }

    public void Stop()
    {
        _isActive = false;
        _transactionTimer.Stop();
    }
}