using Charley.Common;
using System.Data;
using System.Diagnostics;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Charley.Repositories;

public class UserTransactionRepository : IUserTransactionRepository
{
    private readonly SqlProvider _sqlProvider;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly DbProperties _dbProperties;

    public UserTransactionRepository(DbProperties dbProperties,
        SqlProvider sqlProvider,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        TestMetricsService testMetricsService)
    {
        _dbProperties = dbProperties;
        _sqlProvider = sqlProvider;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        testMetricsService?.IncrementClassInstantiation(nameof(UserTransactionRepository));
    }

    public async Task<UserTransaction> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var queryConnection = _dbProperties.CreateQueryConnection();
        var dao = await queryConnection.QuerySingleOrDefaultAsync<UserTransactionDao>(_sqlProvider.GetSql(SqlKeys.GetTransactionById), new { TransactionId = transactionId }, cancellationToken: cancellationToken);
        queryConnection.Close();
        Organization org = null;
        User user = null;

        if (dao != null)
        {
            user = await _userRepository.GetByIdAsync(dao.UserId, cancellationToken);
            org = await _organizationRepository.GetByIdAsync(dao.OrganizationId, cancellationToken);
        }

        return dao?.ToDto(org, user);
    }

    public async Task<IEnumerable<UserTransaction>> GetForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var org = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);

        if (org == null)
        {
            return [];
        }

        using var queryConnection = _dbProperties.CreateQueryConnection();
        var daos = (await queryConnection.QueryAsync<UserTransactionDao>(_sqlProvider.GetSql(SqlKeys.GetTransactionsForOrganization), new
            {
                organizationId
            }, cancellationToken: cancellationToken)).ToArray();

        queryConnection.Close();

        var userIds = daos.Select(d => d.UserId).Distinct().ToArray();
        var users = new User[userIds.Length];
        int i = 0;
        foreach (var id in userIds)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            Debug.Assert(user != null);
            users[i++] = user;
        }

        return daos.Select(d => d.ToDto(org, users.First(u => u.UserId.Equals(d.UserId))));
    }

    public async Task UpsertAsync(UserTransaction transaction, Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var commandConnection = _dbProperties.CreateCommandConnection();
        await commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertTransaction), new UserTransactionDao(transaction, transaction.User.UserId), cancellationToken: cancellationToken);
        commandConnection.Close();
    }

    public async Task<IEnumerable<UserTransaction>> GetTransactionsForOrganizationAsync(Guid orgId, DateTimeOffset start, DateTimeOffset finish, string status = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Fetch the organization
        var organization = await _organizationRepository.GetByIdAsync(orgId, cancellationToken)
            ?? throw new ArgumentException($"Organization with ID {orgId} not found.");

        // Fetch users for the organization
        var users = await _userRepository.GetForCustomerOrganizationAsync(orgId, cancellationToken);
        var userDict = users.ToDictionary(u => u.UserId);

        // Fetch transaction DAOs using a SQL query
        var sql = _sqlProvider.GetSql(SqlKeys.GetTransactionsForOrgWithDateRange);

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND status = @Status";
        }
        var parameters = new { OrgId = orgId, Start = start.ToUniversalTime(), Finish = finish.ToUniversalTime(), Status = status };

        using var queryConnection = _dbProperties.CreateQueryConnection();
        var transactionDaos = await queryConnection.QueryAsync<UserTransactionDao>(sql, parameters, cancellationToken: cancellationToken);
        queryConnection.Close();

        // Map DAOs to DTOs
        var transactionDtos = transactionDaos.Select(dao =>
        {
            if (!userDict.TryGetValue(dao.UserId, out var user))
            {
                throw new InvalidOperationException($"User with ID {dao.UserId} not found for organization {orgId}");
            }
            return dao.ToDto(organization, user);
        });

        return transactionDtos;
    }

    public async Task<IEnumerable<UserTransaction>> GetTransactionsForUserAsync(Guid userId, DateTimeOffset start, DateTimeOffset finish, string status = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Define the SQL query to fetch transactions (assumed to be provided by SqlRepository)
        var sql = _sqlProvider.GetSql(SqlKeys.GetTransactionsForUserWithDateRange);
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND status = @Status";
        }

        // Set up query parameters
        var parameters = new
        {
            UserId = userId,
            Start = start.ToUniversalTime(),
            Finish = finish.ToUniversalTime(),
            Status = status
        };

        using var queryConnection = _dbProperties.CreateQueryConnection();
        var transactionDaos = await queryConnection.QueryAsync<UserTransactionDao>(sql, parameters, cancellationToken: cancellationToken);
        queryConnection.Close();

        // Fetch the User DTO for the given userId (only once, since all transactions are for this user)
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ArgumentException($"User with ID {userId} not found.");

        // Get unique OrganizationIds from the transactions
        var orgIds = transactionDaos.Select(dao => dao.OrganizationId).Distinct().ToList();

        // Fetch all relevant Organization DTOs in a single query
        var organizations = await _organizationRepository.GetByIdsAsync(orgIds, cancellationToken);
        var orgDict = organizations.ToDictionary(org => org.OrganizationId);

        // Map each UserTransactionDao to a UserTransaction DTO
        return transactionDaos.Select(dao =>
        {
            // Retrieve the corresponding organization from the dictionary
            if (!orgDict.TryGetValue(dao.OrganizationId, out var organization))
            {
                throw new InvalidOperationException($"Organization with ID {dao.OrganizationId} not found.");
            }

            // Call ToDto with the fetched organization and user
            return dao.ToDto(organization, user);
        });
    }
}
