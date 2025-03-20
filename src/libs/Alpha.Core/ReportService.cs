using Alpha.Common;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;
namespace Alpha.Core;

public class ReportService : BaseService, IReportService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTransactionRepository _userTransactionRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public ReportService(IOperationContext operationContext,
        IUserRepository userRepository,
        IUserTransactionRepository userTransactionRepository,
        IOrganizationRepository organizationRepository,
        TestMetricsService testMetricsService)
        : base(operationContext, testMetricsService)
    {
        _userRepository = userRepository;
        _userTransactionRepository = userTransactionRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<IEnumerable<Organization>> GetAllOrgsAsync()
    {
        return await _organizationRepository.GetAllOrgsAsync();
    }

    public async Task<IEnumerable<User>> GetUsersForOrgAsync(Guid orgId)
    {
        return await _userRepository.GetForCustomerOrganizationAsync(orgId);
    }

    public async Task<IEnumerable<UserTransaction>> GetTransactionsForOrgAsync(Guid orgId, DateTime? start = null, DateTime? finish = null, string status = null)
    {
        // Set default date range if not provided
        start ??= DateTime.Now.AddMinutes(-15);
        finish ??= DateTime.Now.AddSeconds(-5);

        // Ensure start is before finish
        if (finish < start)
        {
            (start, finish) = (finish, start);
        }

        return await _userTransactionRepository.GetTransactionsForOrganizationAsync(orgId, start.Value, finish.Value, status);
    }

    public async Task<IEnumerable<UserTransaction>> GetTransactionsForUserAsync(Guid userId, DateTime? start = null, DateTime? finish = null, string status = null)
    {
        // Set default date range if not provided
        start ??= DateTime.Now.AddMinutes(-15);
        finish ??= DateTime.Now.AddSeconds(-5);

        // Ensure start is before finish, swapping if necessary
        if (finish < start)
        {
            (start, finish) = (finish, start);
        }

        // Fetch the user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {userId} not found.");
        }

        // Fetch the user's organization
        var organization = await _organizationRepository.GetOrganizationForUserAsync(userId);
        if (organization == null)
        {
            throw new InvalidOperationException($"No organization found for user {userId}");
        }

        // Fetch transactions from the repository
        return await _userTransactionRepository.GetTransactionsForUserAsync(userId, start.Value, finish.Value, status);
    }
}
