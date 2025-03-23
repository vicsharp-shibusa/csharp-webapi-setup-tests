using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Common;

public interface IOrganizationService
{
    Task<IEnumerable<Organization>> GetAllAsync();
    Task<Organization> GetByIdAsync(Guid organizationId);
    Task UpsertAsync(Organization organization, Guid operationId);
}
