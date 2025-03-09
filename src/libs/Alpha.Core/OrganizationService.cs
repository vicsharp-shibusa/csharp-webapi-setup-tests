using Alpha.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Core;

public class OrganizationService : BaseService, IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationService(IOperationContext operationContext,
        IOrganizationRepository organizationRepository,
        TestMetricsService testMetricsService)
        : base(operationContext, testMetricsService)
    {
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

    public Task InsertAsync(Organization organization)
    {
        _testMetricsService.IncrementOrganizations();
        return UpsertAsync(organization);
    }

    public Task UpsertAsync(Organization organization)
    {
        return _organizationRepository.UpsertAsync(organization, GetOperationId().GetValueOrDefault());
    }
}
