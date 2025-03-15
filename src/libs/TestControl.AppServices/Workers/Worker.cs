using System.Net.Http.Json;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices.Workers;

public sealed class Worker
{
    public readonly string Name;
    private readonly HttpClient _httpClient;
    private readonly TestConfig _config;
    private readonly MessageHandler _messageHandler;
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _linkedToken;
    private bool _isActive = false;
    private readonly System.Timers.Timer _transactionTimer;
    private double _transactionCycleTimeLimitMs;
    private readonly Lock _timerLock = new();
    private readonly User _user;

    public Worker(HttpClient httpClient, TestConfig config, MessageHandler messageHandler, User user, CancellationToken cancellationToken)
    {
        Name = $"{nameof(Worker)}-{Guid.NewGuid().ToString()[..8]}";
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _messageHandler = messageHandler;
        _user = user;
        _cts = new CancellationTokenSource();
        _linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token).Token;
        _transactionCycleTimeLimitMs = _config.Workers.WorkerCycleTimeLimitSeconds * 1_000D;

        _transactionTimer = new System.Timers.Timer
        {
            Interval = _config.Workers.WorkerTransactionsRoc.InitialFrequencySeconds * 1_000D,
            AutoReset = true
        };
        _transactionTimer.Elapsed += async (sender, e) => await RunTransactionsAsync();
    }

    public void Initialize()
    {
        _transactionTimer.Start();
        _isActive = true;
    }

    public async Task RunTransactionsAsync()
    {
        if (!_isActive || _linkedToken.IsCancellationRequested)
            return;

        var totalTime = TimeSpan.FromMilliseconds(_transactionCycleTimeLimitMs);
        var startTime = DateTime.Now;

        var transactionsToAdd = new UserTransaction[_config.Workers.TransactionsToCreatePerCycle];
        for (int i = 0; i < _config.Workers.TransactionsToCreatePerCycle; i++)
        {
            transactionsToAdd[i] = TestDataCreationService.CreateTransaction(_user, "Pending");
        }

        foreach (var transaction in transactionsToAdd)
        {
            if (_linkedToken.IsCancellationRequested)
                break;

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/transaction", transaction, _linkedToken);
                response.EnsureSuccessStatusCode();
                TransactionQueue.Enqueue(transaction);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                _messageHandler?.Invoke(new MessageToControlProgram
                {
                    IsTestCancellation = true,
                    Exception = ex,
                    Message = $"Transaction creation failed: {transaction.TransactionId}",
                    MessageLevel = MessageLevel.Error,
                    Source = Name,
                    ThreadId = Environment.CurrentManagedThreadId
                });
            }
        }

        var transactionToUpdate = TransactionQueue.DequeuePendingTransactionForUser(_user);

        if (transactionToUpdate != null)
        {
            var timeLeft = .9D * (totalTime - (DateTime.Now - startTime));

            if (timeLeft > TimeSpan.FromMilliseconds(10))
            {
                await Task.Delay(timeLeft);
            }

            try
            {
                transactionToUpdate.Status = Random.Shared.Next(2) == 0 ? "Approved" : "Denied";
                var response = await _httpClient.PutAsJsonAsync("api/transaction", transactionToUpdate, _linkedToken);
                response.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                _messageHandler?.Invoke(new MessageToControlProgram
                {
                    IsTestCancellation = true,
                    Exception = ex,
                    Message = $"Transaction update failed: {transactionToUpdate.TransactionId}",
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
                Message = $"{Name} could not complete initialization in allotted time.",
                MessageLevel = MessageLevel.Critical,
                Source = Name,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
    }

    public void CompressIntervals()
    {
        var currentInterval = _transactionTimer.Interval;
        var targetInterval = Math.Max(_config.Workers.WorkerTransactionsRoc.MinFrequencySeconds * 1_000D,
                                     currentInterval - _config.Workers.WorkerTransactionsRoc.AmountToDecreaseMs);

        if (targetInterval > 0 && targetInterval < currentInterval)
        {
            lock (_timerLock)
            {
                _transactionCycleTimeLimitMs = targetInterval / 2D;
                _transactionTimer.Stop();
                _transactionTimer.Interval = targetInterval;
                _transactionTimer.Start();
            }
        }
    }

    public void Stop()
    {
        _isActive = false;
        _transactionTimer.Stop();
    }
}