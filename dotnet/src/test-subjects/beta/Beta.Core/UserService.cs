using Beta.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.Core;

public class UserService : BaseService, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTransactionRepository _userTransactionRepository;

    public UserService(IOperationContext operationContext,
        IUserRepository userRepository,
        IUserTransactionRepository userTransactionRepository,
        TestMetricsService testMetricsService)
        : base(operationContext, testMetricsService)
    {
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

    public Task UpsertAsync(User user)
    {
        return _userRepository.UpsertAsync(user, GetOperationId().GetValueOrDefault());
    }

    public Task UpsertTransactionAsync(UserTransaction transaction)
    {
        return _userTransactionRepository.UpsertAsync(transaction, GetOperationId().GetValueOrDefault());
    }
}
