using Alpha.Common;
using Dapper;
using System.Data;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _commandConnection;
        private readonly IDbConnection _queryConnection;
        private readonly DbEngine _dbEngine;
        private readonly SqlProvider _sqlProvider;

        public UserRepository(DbProperties dbProperties,
            SqlProvider sqlProvider,
            TestMetricsService testMetricsService)
        {
            _commandConnection = dbProperties.CommandConnection;
            _queryConnection = dbProperties.QueryConnection;
            _dbEngine = dbProperties.DbEngine;
            testMetricsService?.IncrementClassInstantiation(nameof(UserRepository));
            _sqlProvider = sqlProvider;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            var userId = await _queryConnection.QuerySingleOrDefaultAsync<Guid?>(
                _sqlProvider.GetSql(SqlKeys.GetUserIdForEmail), new { Email = email });

            if (userId.HasValue)
            {
                return await GetByIdAsync(userId.Value);
            }

            return null;
        }

        public async Task<User> GetByIdAsync(Guid userId)
        {
            var orgsForUser = (await _queryConnection.QueryAsync<OrganizationUserDao>(
                _sqlProvider.GetSql(SqlKeys.GetOrganizationsForUser),
                new { userId })).ToArray();

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
                var orgDao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(
                    _sqlProvider.GetSql(SqlKeys.GetOrganizationById),
                    new { OrganizationId = orgId.Value });

                if (orgDao != null && orgDao.ParentOrganizationId.HasValue)
                {
                    var parentOrgDao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(
                        _sqlProvider.GetSql(SqlKeys.GetOrganizationById), new { OrganizationId = orgDao.ParentOrganizationId.Value });

                    parentOrg = parentOrgDao?.ToDto(null);
                }

                userOrg = orgDao?.ToDto(parentOrg);
            }

            var userDao = await _queryConnection.QueryFirstOrDefaultAsync<UserDao>(
                _sqlProvider.GetSql(SqlKeys.GetUserById), new { userId });

            return userDao?.ToDto(userOrg, role);
        }

        public async Task<IEnumerable<User>> GetForustomerOrganizationAsync(Guid customerOrgId)
        {
            return await _queryConnection.QueryAsync<User>(
                _sqlProvider.GetSql(SqlKeys.GetUsersForOrganization), new { CustomerOrgId = customerOrgId });
        }

        public async Task UpsertAsync(User user, Guid operationId)
        {
            if (user.Organization != null)
            {
                if (user.Organization.ParentOrganization != null)
                {
                    await _commandConnection.ExecuteAsync(
                        _sqlProvider.GetSql(SqlKeys.UpsertOrganization),
                        new OrganizationDao(user.Organization.ParentOrganization));
                }
                await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganization),
                    new OrganizationDao(user.Organization));
            }

            await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertUser), new UserDao(user));

            if (user.Organization != null)
            {
                await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganizationUser),
                    new OrganizationUserDao()
                    {
                        OrganizationId = user.Organization.OrganizationId,
                        Role = user.Role ?? "Worker",
                        UserId = user.UserId,
                        JoinedAt = user.CreatedAt
                    });
            }
        }
    }
}
