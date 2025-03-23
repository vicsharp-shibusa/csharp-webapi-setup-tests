using Beta.Common;
using Beta.Repositories;
using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.IntegrationTests;

public class UserTransactionRepositoryTests : TestBase
{
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    public UserTransactionRepositoryTests() : base()
    {
        _userRepository = new UserRepository(DbProperties, _sqlProvider, null);
        var orgRepo = new OrganizationRepository(DbProperties, _sqlProvider, null);

        _transactionRepository = new UserTransactionRepository(DbProperties, _sqlProvider, orgRepo, _userRepository, null);
    }

    [Fact]
    public async Task UpsertAsync_NewTransaction_Inserts()
    {
        var org = TestDataCreationService.CreateOrg(TestDataCreationService.CreateOrg());
        var user = TestDataCreationService.CreateUser(org);
        var transaction = TestDataCreationService.CreateTransaction(user, UserTransactionType.Pending.ToString());

        var opId = Guid.NewGuid();
        await _userRepository.UpsertAsync(user, opId);
        await _transactionRepository.UpsertAsync(transaction, opId);

        var retrieved = await _transactionRepository.GetByIdAsync(transaction.TransactionId);
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.TransactionId, retrieved.TransactionId);
        Assert.Equal(transaction.User.Name, retrieved.User.Name);
        Assert.Equal(transaction.Organization.Name, retrieved.Organization.Name);
        Assert.Equal(transaction.Organization.ParentOrganization.Name, retrieved.Organization.ParentOrganization.Name);
    }

    [Fact]
    public async Task GetForOrganizationAsync_InvalidId_Empty()
    {
        var t = await _transactionRepository.GetForOrganizationAsync(Guid.NewGuid());
        Assert.NotNull(t);
        Assert.Empty(t);
    }

    [Fact]
    public async Task GetForOrganizationAsync_Valid_Returns()
    {
        const int NumToCreate = 10;
        var org = TestDataCreationService.CreateOrg(TestDataCreationService.CreateOrg());
        var user = TestDataCreationService.CreateUser(org);

        await _userRepository.UpsertAsync(user, Guid.NewGuid());

        UserTransaction[] arr = new UserTransaction[NumToCreate];

        for (int i = 0; i < NumToCreate; i++)
        {
            arr[i] = TestDataCreationService.CreateTransaction(user, "Testing");
        }

        foreach (var t in arr)
        {
            await _transactionRepository.UpsertAsync(t, Guid.NewGuid());
        }

        var results = (await _transactionRepository.GetForOrganizationAsync(org.OrganizationId)).ToArray();

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Equal(arr.Length, results.Length);
    }
}
