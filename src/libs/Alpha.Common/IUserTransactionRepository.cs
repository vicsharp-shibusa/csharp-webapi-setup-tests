using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Common;

public interface IUserTransactionRepository
{
    Task<UserTransaction> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<UserTransaction>> GetForOrganizationAsync(Guid organizationId);
    Task UpsertAsync(UserTransaction transaction, Guid operationId);
    Task<IEnumerable<UserTransaction>> GetTransactionsForOrganizationAsync(Guid orgId, DateTimeOffset start, DateTimeOffset finish, string status = null);
    Task<IEnumerable<UserTransaction>> GetTransactionsForUserAsync(Guid userId, DateTimeOffset start, DateTimeOffset finish, string status = null);
}
