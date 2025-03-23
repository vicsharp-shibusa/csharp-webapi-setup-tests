using Charley.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Core;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTransactionRepository _userTransactionRepository;

    public UserService(
        IUserRepository userRepository,
        IUserTransactionRepository userTransactionRepository,
        TestMetricsService testMetricsService)
    {
        testMetricsService.IncrementClassInstantiation(nameof(UserService));
        _userRepository = userRepository;
        _userTransactionRepository = userTransactionRepository;
    }

    public Task<User> GetByEmailAsync(string email)
    {
        return _userRepository.GetByEmailAsync(email);
    }

    public Task<User> GetByIdAsync(Guid userId)
    {
        return _userRepository.GetByIdAsync(userId);
    }

    public Task<IEnumerable<User>> GetForOrganizationAsync(Guid customerOrgId)
    {
        return _userRepository.GetForCustomerOrganizationAsync(customerOrgId);
    }

    public Task UpsertAsync(User user, Guid operationId)
    {
        return _userRepository.UpsertAsync(user, operationId);
    }

    public Task UpsertTransactionAsync(UserTransaction transaction, Guid operationId)
    {
        return _userTransactionRepository.UpsertAsync(transaction, operationId);
    }
}
