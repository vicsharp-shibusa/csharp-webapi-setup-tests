using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.Common;

public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetForCustomerOrganizationAsync(Guid customerOrgId, CancellationToken cancellationToken = default);
    Task UpsertAsync(User user, Guid operationId,CancellationToken cancellationToken = default);
}
