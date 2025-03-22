using Alpha.Common;
using System.Data;
using TestControl.AppServices;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;
using TestControl.Infrastructure;

namespace Alpha.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly IDbConnection _commandConnection;
    private readonly IDbConnection _queryConnection;
    private readonly DbEngine _dbEngine;
    private readonly SqlProvider _sqlProvider;

    public OrganizationRepository(DbProperties dbProperties, SqlProvider sqlProvider, TestMetricsService testMetricsService)
    {
        testMetricsService?.IncrementClassInstantiation(nameof(OrganizationRepository));
        _commandConnection = dbProperties.CommandConnection;
        _queryConnection = dbProperties.QueryConnection;
        _dbEngine = dbProperties.DbEngine;
        _sqlProvider = sqlProvider;
    }

    public async Task<IEnumerable<Organization>> GetAllOrgsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var orgDaos = (await _queryConnection.QueryAsync<OrganizationDao>(
            _sqlProvider.GetSql(SqlKeys.GetAllOrganizations), cancellationToken: cancellationToken)).ToList();
        _queryConnection.Close(); // Close early
        var orgDict = new Dictionary<Guid, Organization>();

        orgDaos.ForEach(dao => orgDict[dao.OrganizationId] = dao.ToDto()); // hydrate dictionary
        orgDaos.ForEach(dao =>
        {
            var org = orgDict[dao.OrganizationId];
            if (dao.ParentOrganizationId.HasValue &&
                orgDict.TryGetValue(dao.ParentOrganizationId.Value, out var parentOrg)) // find parent in dictionary
            {
                org.ParentOrganization = parentOrg;
            }
        });

        return orgDict.Values;
    }

    public async Task<Organization> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parentOrgId = await _queryConnection.QuerySingleOrDefaultAsync<Guid?>(_sqlProvider.GetSql(SqlKeys.GetParentOrgIdForOrgId), new { organizationId }, cancellationToken: cancellationToken);

        Organization parentOrg = null;
        if (parentOrgId.HasValue)
        {
            parentOrg = await GetByIdAsync(parentOrgId.Value, cancellationToken);
        }

        return (await _queryConnection.QuerySingleOrDefaultAsync<OrganizationDao>(_sqlProvider.GetSql(SqlKeys.GetOrganizationById), new { OrganizationId = organizationId }, cancellationToken: cancellationToken))?.ToDto(parentOrg);
        // No close - scope handles it
    }

    public async Task UpsertAsync(Organization organization, Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (organization.ParentOrganization != null)
        {
            await UpsertAsync(organization.ParentOrganization, operationId, cancellationToken);
        }

        await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganization), new OrganizationDao(organization), cancellationToken: cancellationToken);
        // No close - scope handles it
    }

    public async Task<Organization> GetOrganizationForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = _sqlProvider.GetSql(SqlKeys.GetOrganizationForUser);
        var parameters = new { UserId = userId };
        var dao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(sql, parameters, cancellationToken: cancellationToken);
        _queryConnection.Close(); // Close early
        return dao?.ToDto();
    }

    public async Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> orgIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = _dbEngine == DbEngine.MSSQL
            ? "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations WHERE OrganizationId IN @OrgIds"
            : "SELECT organizationid, name, parentorganizationid, createdat FROM organizations WHERE organizationid = ANY(@OrgIds)";
        var parameters = new { OrgIds = orgIds.ToList() };
        var daos = await _queryConnection.QueryAsync<OrganizationDao>(sql, parameters, cancellationToken: cancellationToken);
        _queryConnection.Close(); // Close early
        return daos.Select(dao => dao.ToDto());
    }
}