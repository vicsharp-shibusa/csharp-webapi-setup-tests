using Beta.Common;
using Beta.Repositories;
using TestControl.Infrastructure;

namespace Beta.IntegrationTests;

public class OrganizationRepositoryTests : TestBase
{

    private readonly IOrganizationRepository _orgRepository;

    public OrganizationRepositoryTests() : base()
    {
        _orgRepository = new OrganizationRepository(DbProperties, _sqlProvider, null);
    }

    [Fact]
    public async Task UpsertAsync_New_Inserts()
    {
        var org = TestDataCreationService.CreateOrg();

        Assert.Null(org.ParentOrganization);

        await _orgRepository.UpsertAsync(org, Guid.NewGuid());

        var retrieved = await _orgRepository.GetByIdAsync(org.OrganizationId);

        Assert.Equal(org.OrganizationId, retrieved.OrganizationId);
        Assert.Null(retrieved.ParentOrganization);
    }

    [Fact]
    public async Task UpsertAsync_NewWithParent_Inserts()
    {
        var org = TestDataCreationService.CreateOrg(TestDataCreationService.CreateOrg());

        Assert.NotNull(org.ParentOrganization);

        await _orgRepository.UpsertAsync(org, Guid.NewGuid());

        var retrieved = await _orgRepository.GetByIdAsync(org.OrganizationId);

        Assert.Equal(org.OrganizationId, retrieved.OrganizationId);
        Assert.NotNull(retrieved.ParentOrganization);
        Assert.Equal(org.ParentOrganization.OrganizationId, retrieved.ParentOrganization.OrganizationId);
    }
}
