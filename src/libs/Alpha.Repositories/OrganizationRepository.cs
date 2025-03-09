using Alpha.Common;
using Dapper;
using System.Data;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly IDbConnection _commandConnection;
    private readonly IDbConnection _queryConnection;
    private readonly DbEngine _dbEngine;
    private readonly SqlProvider _sqlProvider;

    public OrganizationRepository(DbProperties dbProperties,
        SqlProvider sqlProvider,
        TestMetricsService testMetricsService)
    {
        _commandConnection = dbProperties.CommandConnection;
        _queryConnection = dbProperties.QueryConnection;
        _dbEngine = dbProperties.DbEngine;
        testMetricsService?.IncrementClassInstantiation(nameof(OrganizationRepository));
        _sqlProvider = sqlProvider;
    }

    public async Task<IEnumerable<Organization>> GetAllOrgsAsync()
    {
        var orgDaos = await _queryConnection.QueryAsync<OrganizationDao>(_sqlProvider.GetSql(SqlKeys.GetAllOrganizations));

        var orgDict = new Dictionary<Guid, Organization>();

        foreach (var dao in orgDaos)
        {
            orgDict[dao.OrganizationId] = dao.ToDto();
        }

        foreach (var dao in orgDaos)
        {
            var org = orgDict[dao.OrganizationId];
            if (dao.ParentOrganizationId.HasValue &&
                orgDict.TryGetValue(dao.ParentOrganizationId.Value, out var parentOrg))
            {
                org.ParentOrganization = parentOrg;
            }
        }

        return orgDict.Values;
    }

    public async Task<Organization> GetByIdAsync(Guid organizationId)
    {
        var parentOrgId = await _queryConnection.QuerySingleOrDefaultAsync<Guid?>(
            _sqlProvider.GetSql(SqlKeys.GetParentOrgIdForOrgId), new { organizationId });

        Organization parentOrg = null;
        if (parentOrgId.HasValue)
        {
            parentOrg = await GetByIdAsync(parentOrgId.Value);
        }

        return (await _queryConnection.QuerySingleOrDefaultAsync<OrganizationDao>(
            _sqlProvider.GetSql(SqlKeys.GetOrganizationById), new { OrganizationId = organizationId }))?.ToDto(parentOrg);
    }

    public async Task UpsertAsync(Organization organization, Guid operationId)
    {
        if (organization.ParentOrganization != null)
        {
            await UpsertAsync(organization.ParentOrganization, operationId);
        }
        await _commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.UpsertOrganization), new OrganizationDao(organization));
    }

    public async Task<Organization> GetOrganizationForUserAsync(Guid userId)
    {
        var sql = _sqlProvider.GetSql(SqlKeys.GetOrganizationForUser);
        var parameters = new { UserId = userId };
        var dao = await _queryConnection.QueryFirstOrDefaultAsync<OrganizationDao>(sql, parameters);
        return dao?.ToDto();
    }

    public async Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> orgIds)
    {
        // Define the SQL query based on the database engine
        var sql = _dbEngine == DbEngine.MSSQL
            ? "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations WHERE OrganizationId IN @OrgIds"
            : "SELECT organizationid, name, parentorganizationid, createdat FROM organizations WHERE organizationid = ANY(@OrgIds)";

        // Execute the query with the list of OrgIds
        var parameters = new { OrgIds = orgIds.ToList() };
        var daos = await _queryConnection.QueryAsync<OrganizationDao>(sql, parameters);

        // Map DAOs to DTOs
        return daos.Select(dao => dao.ToDto());
    }
}
