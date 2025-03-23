using Charley.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Core;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    
    public OrganizationService(
        IOrganizationRepository organizationRepository,
        TestMetricsService testMetricsService)
    {
        testMetricsService.IncrementClassInstantiation(nameof(OrganizationService));
        _organizationRepository = organizationRepository;
    }

    public Task<IEnumerable<Organization>> GetAllAsync()
    {
        return _organizationRepository.GetAllOrgsAsync();
    }

    public Task<Organization> GetByIdAsync(Guid organizationId)
    {
        return _organizationRepository.GetByIdAsync(organizationId);
    }

    public Task UpsertAsync(Organization organization, Guid operationId)
    {
        return _organizationRepository.UpsertAsync(organization, operationId);
    }
}
