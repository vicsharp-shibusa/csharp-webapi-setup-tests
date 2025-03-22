using Alpha.Common;
using Alpha.Repositories;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Test.Alpha.IntegrationTests;

public class UserRepositoryTests : TestBase
{
    private readonly IUserRepository _userRepository;

    public UserRepositoryTests() : base()
    {
        _userRepository = new UserRepository(DbProperties, _sqlProvider, null);
    }

    [Fact]
    public async Task UpsertUserAsync_NewUserNoOrg_Inserts()
    {
        var user = TestDataCreationService.CreateUser();

        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByIdAsync(user.UserId);
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.UserId, retrievedUser.UserId);
        Assert.Equal(user.Email, retrievedUser.Email);
    }

    [Fact]
    public async Task UpsertUserAsync_NewUserWithOrg_InsertsUserAndOrg()
    {
        var user = TestDataCreationService.CreateUser(new Organization
        {
            Name = $"org_{TestDataCreationService.GetUniqueString(6)}"
        });

        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByIdAsync(user.UserId);
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.UserId, retrievedUser.UserId);
        Assert.NotNull(retrievedUser.Organization);
        Assert.Equal(user.Organization.OrganizationId, retrievedUser.Organization.OrganizationId);
    }

    [Fact]
    public async Task UpsertUserAsync_NewUserWithOrgAndParent_InsertsUserAndOrgAndParent()
    {
        var user = TestDataCreationService.CreateUser(new Organization
        {
            Name = $"org_{TestDataCreationService.GetUniqueString(6)}",
            ParentOrganization = new Organization
            {
                Name = $"parentorg_{TestDataCreationService.GetUniqueString(6)}",
            }
        });

        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByIdAsync(user.UserId);
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.UserId, retrievedUser.UserId);
        Assert.NotNull(retrievedUser.Organization);
        Assert.NotNull(retrievedUser.Organization.ParentOrganization);
        Assert.Equal(user.Organization.OrganizationId,
            retrievedUser.Organization.OrganizationId);
        Assert.Equal(user.Organization.ParentOrganization.OrganizationId,
            retrievedUser.Organization.ParentOrganization.OrganizationId);
    }

    [Fact]
    public async Task UpsertUserAsync_ExistingUserWithOrgAndParent_InsertsUserAndOrgAndParent()
    {
        var user = TestDataCreationService.CreateUser(new Organization
        {
            Name = $"org_{TestDataCreationService.GetUniqueString(6)}",
            ParentOrganization = new Organization
            {
                Name = $"parentorg_{TestDataCreationService.GetUniqueString(6)}",
            }
        });

        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var userToUpdate = new User()
        {
            UserId = user.UserId,
            Email = $"updated-{TestDataCreationService.GetUniqueString()}@test.org".ToLower(),
            Name = $"Updated {TestDataCreationService.GetUniqueString()}",
            Role = "Other",
            Organization = new Organization()
            {
                OrganizationId = user.Organization.OrganizationId,
                Name = $"Updated {TestDataCreationService.GetUniqueString()}",
                ParentOrganization = new Organization
                {
                    OrganizationId = user.Organization.ParentOrganization.OrganizationId,
                    Name = $"Updated Parent {TestDataCreationService.GetUniqueString()}"
                }
            }
        };

        await _userRepository.UpsertAsync(userToUpdate, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByIdAsync(userToUpdate.UserId);
        Assert.NotNull(retrievedUser);
        Assert.Equal(userToUpdate.UserId, retrievedUser.UserId);
        Assert.NotNull(userToUpdate.Organization);
        Assert.NotNull(userToUpdate.Organization.ParentOrganization);
        Assert.Equal(userToUpdate.Organization.OrganizationId,
            retrievedUser.Organization.OrganizationId);
        Assert.Equal(userToUpdate.Organization.ParentOrganization.OrganizationId,
            retrievedUser.Organization.ParentOrganization.OrganizationId);
        Assert.Equal(userToUpdate.Name, retrievedUser.Name);
        Assert.Equal(userToUpdate.Email, retrievedUser.Email);
        Assert.Equal(userToUpdate.Organization.Name,
            retrievedUser.Organization.Name);
        Assert.Equal(userToUpdate.Organization.ParentOrganization.Name,
            retrievedUser.Organization.ParentOrganization.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_ValidEmail_Finds()
    {
        var user = TestDataCreationService.CreateUser();
        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByEmailAsync(user.Email);
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.UserId, retrievedUser.UserId);
    }

    [Fact]
    public async Task GetByEmailAsync_InvalidEmail_NoResults()
    {
        var retrievedUser = await _userRepository.GetByEmailAsync("nonexistent@example.com");
        Assert.Null(retrievedUser);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_Finds()
    {
        var user = TestDataCreationService.CreateUser();
        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        var retrievedUser = await _userRepository.GetByIdAsync(user.UserId);
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.UserId, retrievedUser.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_NoResults()
    {
        var retrievedUser = await _userRepository.GetByIdAsync(Guid.NewGuid());
        Assert.Null(retrievedUser);
    }

    [Fact]
    public async Task GetForOrganizationAsync_ValidOrg_Finds()
    {
        var org = new Organization()
        {
            Name = TestDataCreationService.GetUniqueString()
        };

        var users = new User[] {
            TestDataCreationService.CreateUser(org),
            TestDataCreationService.CreateUser(org),
            TestDataCreationService.CreateUser(org)
        };

        foreach (var u in users)
        {
            await _userRepository.UpsertAsync(u, Guid.NewGuid());
        }

        var fromDb = (await _userRepository.GetForCustomerOrganizationAsync(org.OrganizationId)).ToArray();

        Assert.NotNull(fromDb);
        Assert.Equal(users.Length, fromDb.Length);
    }

    [Fact]
    public async Task GetForOrganizationAsync_InvalidOrg_NoResults()
    {
        var usersInOrg = await _userRepository.GetForCustomerOrganizationAsync(Guid.NewGuid());
        Assert.NotNull(usersInOrg);
        Assert.Empty(usersInOrg);
    }
}
