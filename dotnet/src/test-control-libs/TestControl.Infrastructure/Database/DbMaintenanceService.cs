using Microsoft.Data.SqlClient;
using Npgsql;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.Infrastructure.Database;

/// <summary>
/// Utility class for db maintenance.
/// </summary>
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

    public async Task PurgeDatabase()
    {
        using var commandConnection = _dbProperties.CreateCommandConnection();
        await commandConnection.ExecuteAsync(_sqlProvider.GetSql(SqlKeys.DeleteAllData),
            commandTimeout: 120);
        commandConnection.Close();
    }

    public async Task<TestStatusCounts> CountRows()
    {
        using var queryConnection = _dbProperties.CreateQueryConnection();
        var counts = await queryConnection.QueryFirstOrDefaultAsync<TestStatusCounts>(
            _sqlProvider.GetSql(SqlKeys.GetTableCounts));
        queryConnection.Close();
        return counts;
    }


    public static bool IsTransientNpgsqlError(NpgsqlException ex)
    {
        return ex.SqlState == "08006" // Connection failure
            || ex.SqlState == "53300" // Too many connections
            || ex.SqlState == "08001"; // Invalid connection
    }

    public static bool IsTransientSqlError(SqlException ex)
    {
        return ex.Number == -2     // Timeout
            || ex.Number == 1205   // Deadlock
            || ex.Number == 10053; // Network-related error
    }
}
