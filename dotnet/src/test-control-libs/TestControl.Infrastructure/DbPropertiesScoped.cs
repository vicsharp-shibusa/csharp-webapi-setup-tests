using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;

namespace TestControl.Infrastructure;

public class DbPropertiesScoped : IDisposable
{
    public string DbVersion { get; init; }
    public DbEngine DbEngine { get; init; }
    public IDbConnection CommandConnection { get; init; }
    public IDbConnection QueryConnection { get; init; }
    public static async Task<IDbTransaction> CreateTransactionAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (connection.State != ConnectionState.Open)
            throw new Exception("Connection must be in an OPEN state.");

        if (connection is SqlConnection sqlConn)
            return await sqlConn.BeginTransactionAsync(cancellationToken);
        else if (connection is NpgsqlConnection npgConn)
            return await npgConn.BeginTransactionAsync(cancellationToken);
        return connection?.BeginTransaction();
    }

    public static IDbTransaction CreateTransaction(IDbConnection connection) => connection.BeginTransaction();

    public void Dispose()
    {
        CommandConnection?.Dispose();
        QueryConnection?.Dispose();
    }
}