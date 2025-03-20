using Microsoft.Data.SqlClient;
using Npgsql;
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

    public static bool IsTransientNpgsqlError(NpgsqlException ex)
    {
        // Retry on connection failures, timeouts, or too many connections
        return ex.SqlState == "08006" // Connection failure
            || ex.SqlState == "53300" // Too many connections
            || ex.SqlState == "08001"; // Invalid connection
    }

    public static bool IsTransientSqlError(SqlException ex)
    {
        // Retry on timeouts, deadlocks, or network errors
        return ex.Number == -2     // Timeout
            || ex.Number == 1205   // Deadlock
            || ex.Number == 10053; // Network-related error
    }
}
