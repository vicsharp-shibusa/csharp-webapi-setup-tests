using TestControl.Infrastructure.SubjectApiPublic;
namespace Alpha.Common;

public interface IOrganizationRepository
{
    Task<IEnumerable<Organization>> GetAllOrgsAsync();
    Task<Organization> GetByIdAsync(Guid organizationId);
    Task UpsertAsync(Organization organization, Guid operationId);
    Task<Organization> GetOrganizationForUserAsync(Guid userId);
    Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> orgIds);
}