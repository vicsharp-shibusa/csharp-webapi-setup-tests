using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.Common;

public interface IUserService
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByIdAsync(Guid userId);
    Task<IEnumerable<User>> GetForOrganizationAsync(Guid customerOrgId);
    Task UpsertAsync(User user);
    Task UpsertTransactionAsync(UserTransaction transaction);
}