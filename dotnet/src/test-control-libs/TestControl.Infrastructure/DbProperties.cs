using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;

namespace TestControl.Infrastructure;

public class DbProperties
{
    private readonly string _commandConnectionString;
    private readonly string _queryConnectionString;

    public DbProperties(string commandConnectionString, string queryConnectionString, string dbVersion, DbEngine engine)
    {
        _commandConnectionString = commandConnectionString;
        _queryConnectionString = queryConnectionString;
        DbVersion = dbVersion;
        DbEngine = engine;
    }


    public string DbVersion { get; init; }
    public DbEngine DbEngine { get; init; }

    public IDbConnection CreateCommandConnection() =>
        DbEngine switch
        {
            DbEngine.MSSQL => new SqlConnection(_commandConnectionString),
            DbEngine.PostgreSQL => new NpgsqlConnection(_commandConnectionString),
            _ => throw new InvalidOperationException($"Unknown Db Engine: {DbEngine}")
        };

    public IDbConnection CreateQueryConnection() =>
        DbEngine switch
        {
            DbEngine.MSSQL => new SqlConnection(_queryConnectionString),
            DbEngine.PostgreSQL => new NpgsqlConnection(_queryConnectionString),
            _ => throw new InvalidOperationException($"Unknown Db Engine: {DbEngine}")
        };

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

}