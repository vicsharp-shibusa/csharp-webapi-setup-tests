using TestControl.Infrastructure.SubjectApiPublic;
namespace Alpha.Common;

public interface IOrganizationRepository
{
    Task<IEnumerable<Organization>> GetAllOrgsAsync(CancellationToken cancellationToken = default);
    Task<Organization> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task UpsertAsync(Organization organization, Guid operationId,CancellationToken cancellationToken = default);
    Task<Organization> GetOrganizationForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> orgIds, CancellationToken cancellationToken = default);
}