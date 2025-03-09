//using System.Transactions;
//using TestControl.Infrastructure;

//namespace TestControl.AppServices.Workers;

//public class Worker
//{
//    private readonly TestConfig _config;
//    private readonly List<Transaction> _transactions;

//    public Worker(TestConfig config)
//    {
//        _config = config ?? throw new ArgumentNullException(nameof(config));
//        _transactions = new List<Transaction>();
//    }

//    public async Task ProcessTransactionsAsync()
//    {
//        // Create transactions
//        for (int i = 0; i < _config.FrequencyControl.TransactionProcessing.usert.Tra .TransactionProcessing.UserTransactionsToCreatePerCycle; i++)
//        {
//            var transaction = new Transaction
//            {
//                Id = Guid.NewGuid(),
//                CreatedDate = DateTime.Now
//                // Add other transaction properties as needed
//            };
//            _transactions.Add(transaction);
//        }

//        // Review transactions
//        for (int i = 0; i < Math.Min(_config.TransactionProcessing.UserTransactionsToReviewPerCycle, _transactions.Count); i++)
//        {
//            var transaction = _transactions[i];
//            transaction.ReviewedDate = DateTime.Now;
//            // Add review logic as needed
//        }

//        // Simulate async processing
//        await Task.CompletedTask;
//    }

//    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();
//}

////using System;
////using System.Net.Http.Json;
////using System.Threading;
////using System.Threading.Tasks;
////using TestControl.Infrastructure;
////using TestControl.Infrastructure.SubjectApiPublic;

////namespace TestControl.AppServices.Workers
////{
////    public class Worker
////    {
////        private readonly HttpClient _httpClient;
////        private readonly TestConfig _config;
////        private readonly MessageHandler _messageHandler;
////        private readonly CancellationToken _cancellationToken;
////        private readonly User _user;
////        private readonly Organization _organization;
////        private bool _isActive = false;

////        public Worker(HttpClient httpClient, TestConfig config, MessageHandler messageHandler, CancellationToken cancellationToken, User user, Organization organization)
////        {
////            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
////            _config = config ?? throw new ArgumentNullException(nameof(config));
////            _messageHandler = messageHandler;
////            _cancellationToken = cancellationToken;
////            _user = user ?? throw new ArgumentNullException(nameof(user));
////            _organization = organization ?? throw new ArgumentNullException(nameof(organization));
////        }

////        public Task ActivateAsync()
////        {
////            _isActive = true;
////            return Task.CompletedTask;
////        }

////        public async Task ProcessTransactionsAsync()
////        {
////            if (!_isActive || _cancellationToken.IsCancellationRequested)
////                return;

////            for (int i = 0; i < _config.FrequencyControl.TransactionProcessing.UserTransactionsToCreatePerCycle; i++)
////            {
////                var transaction = await FetchOrCreateTransactionAsync();
////                if (transaction != null)
////                {
////                    await UpdateTransactionStatusAsync(transaction.TransactionId, "In Review");
////                    await PerformComplexComputationAsync();
////                    await UpdateTransactionStatusAsync(transaction.TransactionId, ApproveOrDeny(transaction.Amount));
////                }
////            }
////        }

////        private async Task<UserTransaction> FetchOrCreateTransactionAsync()
////        {
////            try
////            {
////                // Attempt to fetch pending transactions for the organization
////                var transactions = await _httpClient.GetFromJsonAsync<List<UserTransaction>>(
////                    $"api/transactions/pending?organizationId={_organization.OrganizationId}",
////                    _cancellationToken
////                );
////                var pending = transactions?.FirstOrDefault(t => t.Status == "Pending");
////                if (pending != null)
////                    return pending;

////                // If no pending transactions, create a new one
////                var newTransaction = TestDataCreationService.CreateUserTransaction(_user, _organization, "Pending");
////                var response = await _httpClient.PostAsJsonAsync("api/transactions", newTransaction, _cancellationToken);
////                response.EnsureSuccessStatusCode();
////                return await response.Content.ReadFromJsonAsync<UserTransaction>(_cancellationToken);
////            }
////            catch (Exception ex)
////            {
////                _messageHandler?.Invoke(new MessageToControlProgram
////                {
////                    Exception = ex,
////                    Message = "Failed to fetch or create transaction",
////                    MessageLevel = MessageLevel.Err,
////                    Source = nameof(Worker),
////                    ThreadId = Environment.CurrentManagedThreadId
////                });
////                return null;
////            }
////        }

////        private async Task UpdateTransactionStatusAsync(Guid transactionId, string newStatus)
////        {
////            var updateData = new { Status = newStatus };
////            try
////            {
////                var response = await _httpClient.PutAsJsonAsync(
////                    $"api/transactions/{transactionId}/status",
////                    updateData,
////                    _cancellationToken
////                );
////                response.EnsureSuccessStatusCode();
////                _messageHandler?.Invoke(new MessageToControlProgram
////                {
////                    Message = $"Updated transaction {transactionId} to {newStatus}",
////                    MessageLevel = MessageLevel.Info,
////                    Source = nameof(Worker),
////                    ThreadId = Environment.CurrentManagedThreadId
////                });
////            }
////            catch (Exception ex)
////            {
////                _messageHandler?.Invoke(new MessageToControlProgram
////                {
////                    Exception = ex,
////                    Message = $"Failed to update transaction {transactionId}",
////                    MessageLevel = MessageLevel.Err,
////                    Source = nameof(Worker),
////                    ThreadId = Environment.CurrentManagedThreadId
////                });
////            }
////        }

////        private async Task PerformComplexComputationAsync()
////        {
////            int complexity = ComputeHashDifficulty();
////            Parallel.For(0, complexity, i => { _ = Math.Pow(i, 2); });
////            await Task.Delay(TimeSpan.FromMilliseconds(complexity), _cancellationToken);
////        }

////        private int ComputeHashDifficulty()
////        {
////            return _config.FrequencyControl.TransactionProcessing.MaxTimeToMinFrequencyMinutes * 100;
////        }

////        private static string ApproveOrDeny(decimal amount)
////        {
////            return amount % 2 == 0 ? "Approved" : "Denied";
////        }
////    }
////}