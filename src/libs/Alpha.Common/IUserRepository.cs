using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Common;

public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByIdAsync(Guid userId);
    Task<IEnumerable<User>> GetForustomerOrganizationAsync(Guid customerOrgId);
    Task UpsertAsync(User user, Guid operationId);
}
