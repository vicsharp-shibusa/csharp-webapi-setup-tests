using Alpha.Common;
using System.Data;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _commandConnection;
    private readonly IDbConnection _queryConnection;
    private readonly SqlProvider _sqlProvider;

    public UserRepository(DbProperties dbProperties,
        SqlProvider sqlProvider,
        TestMetricsService testMetricsService)
    {
        testMetricsService?.IncrementClassInstantiation(nameof(UserRepository));
        _commandConnection = dbProperties.CommandConnection;
        _queryConnection = dbProperties.QueryConnection;
        _sqlProvider = sqlProvider;
    }

    public async Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = await _queryConnection.QuerySingleOrDefaultAsync<Guid?>(_sqlProvider.GetSql(SqlKeys.GetUserIdForEmail), new { Email = email }, cancellationToken: cancellationToken);

        if (userId.HasValue)
        {
            return await GetByIdAsync(userId.Value, cancellationToken);
        }

        return null;
    }

    public async Task<User> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var orgsForUser = (await _queryConnection.QueryAsync<OrganizationUserDao>(_sqlProvider.GetSql(SqlKeys.GetOrganizationsForUser), new { userId }, cancellationToken: cancellationToken)).ToArray();

        Guid? orgId = null;
        string role = null;

        if (orgsForUser.Length > 0)
        {
            orgId = orgsForUser[0].OrganizationId;
            role = orgsForUser[0].Role;
        }

        Organization userOrg = null;
        Organization parentOrg = null;

        if (orgId.HasValue)
        {
            var orgDao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(_sqlProvider.GetSql(SqlKeys.GetOrganizationById), new { OrganizationId = orgId.Value }, cancellationToken: cancellationToken);

            if (orgDao != null && orgDao.ParentOrganizationId.HasValue)
            {
                var parentOrgDao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(_sqlProvider.GetSql(SqlKeys.GetOrganizationById), new { OrganizationId = orgDao.ParentOrganizationId.Value }, cancellationToken: cancellationToken);

                parentOrg = parentOrgDao?.ToDto(null);
            }

            userOrg = orgDao?.ToDto(parentOrg);
        }

        var userDao = await _queryConnection.QueryFirstOrDefaultAsync<UserDao>(_sqlProvider.GetSql(SqlKeys.GetUserById), new { userId }, cancellationToken: cancellationToken);

        _queryConnection.Close();
        return userDao?.ToDto(userOrg, role);
    }

    public async Task<IEnumerable<User>> GetForCustomerOrganizationAsync(Guid customerOrgId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await _queryConnection.QueryAsync<UserDao>(_sqlProvider.GetSql(SqlKeys.GetUsersForOrganization), new { CustomerOrgId = customerOrgId }, cancellationToken: cancellationToken))
            .Select(k => k.ToDto());
    }

    public async Task UpsertAsync(User user, Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _commandConnection.EnsureOpenConnectionAsync(cancellationToken);

        using (var transaction = await DbProperties.CreateTransactionAsync(_commandConnection, cancellationToken))
        {
            try
            {
                if (user.Organization != null)
                {
                    if (user.Organization.ParentOrganization != null)
                    {
                        await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganization), new OrganizationDao(user.Organization.ParentOrganization), transaction, cancellationToken: cancellationToken);
                    }
                    await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganization), new OrganizationDao(user.Organization), transaction, cancellationToken: cancellationToken);
                }

                await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertUser), new UserDao(user), transaction, cancellationToken: cancellationToken);

                if (user.Organization != null)
                {
                    await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganizationUser), new OrganizationUserDao()
                        {
                            OrganizationId = user.Organization.OrganizationId,
                            Role = user.Role ?? "Worker",
                            UserId = user.UserId,
                            JoinedAt = user.CreatedAt.UtcDateTime
                        }, transaction, cancellationToken: cancellationToken);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                _commandConnection.Close();
            }
        }
    }
}
