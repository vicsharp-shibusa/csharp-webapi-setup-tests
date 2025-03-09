using Dapper;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.Infrastructure.Database;

public class DbMaintenanceService
{
    private readonly DbProperties _dbProperties;
    private readonly SqlProvider _sqlProvider;

    public DbMaintenanceService(DbProperties dbProperties,
        SqlProvider sqlProvider)
    {
        _dbProperties = dbProperties;
        _sqlProvider = sqlProvider;
    }

    public Task PurgeDatabase() => _dbProperties.CommandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.DeleteAllData),
            commandTimeout: 120);

    public Task<TestStatusCounts> CountRows() =>
        _dbProperties.QueryConnection.QueryFirstOrDefaultAsync<TestStatusCounts>(
            _sqlProvider.GetSql(SqlKeys.GetTableCounts));
}
