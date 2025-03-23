using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Common;

public interface IUserService
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByIdAsync(Guid userId);
    Task<IEnumerable<User>> GetForOrganizationAsync(Guid customerOrgId);
    Task UpsertAsync(User user, Guid operationId);
    Task UpsertTransactionAsync(UserTransaction transaction, Guid operationId);
}