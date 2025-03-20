using Alpha.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Core;

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

    public async Task UpsertAsync(User user)
    {
        await _userRepository.UpsertAsync(user, GetOperationId().GetValueOrDefault());
    }

    public async Task UpsertTransactionAsync(UserTransaction transaction)
    {
        await _userTransactionRepository.UpsertAsync(transaction, GetOperationId().GetValueOrDefault());
    }
}
