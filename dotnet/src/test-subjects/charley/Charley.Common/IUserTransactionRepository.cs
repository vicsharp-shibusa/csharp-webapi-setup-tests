using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Common;

public interface IUserTransactionRepository
{
    Task<UserTransaction> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserTransaction>> GetForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task UpsertAsync(UserTransaction transaction, Guid operationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserTransaction>> GetTransactionsForOrganizationAsync(Guid orgId, DateTimeOffset start, DateTimeOffset finish, string status = null, CancellationToken cancellation = default);
    Task<IEnumerable<UserTransaction>> GetTransactionsForUserAsync(Guid userId, DateTimeOffset start, DateTimeOffset finish, string status = null, CancellationToken cancellationToken = default);
}
