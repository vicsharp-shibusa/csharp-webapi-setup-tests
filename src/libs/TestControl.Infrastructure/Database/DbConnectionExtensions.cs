using System.Data;
using System.Data.Common;

namespace TestControl.Infrastructure.Database;

public static class DbConnectionExtensions
{
    private const int DefaultTimeout = 30;

    public static void EnsureOpenConnection(this IDbConnection connection)
    {
        if (connection.State == ConnectionState.Closed)
            connection.Open();
    }

    public static async Task EnsureOpenConnectionAsync(this IDbConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (connection.State == ConnectionState.Closed)
        {
            if (connection is DbConnection dbConn)
                await dbConn.OpenAsync(cancellationToken);
            else
                connection.Open();
        }
    }

    public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            EnsureOpenConnection(connection);

            using (var reader = command.ExecuteReader())
            {
                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    while (reader.Read())
                    {
                        var value = reader.GetValue(0);
                        yield return reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                    }
                }
                else
                {
                    var mapper = CreateMapper<T>(reader);
                    while (reader.Read())
                    {
                        var item = Activator.CreateInstance<T>();
                        mapper(item, reader);
                        yield return item;
                    }
                }
            }
        }
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, string sql, object param = null,
        IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            await EnsureOpenConnectionAsync(connection, cancellationToken);

            using (var reader = command is DbCommand dbCmd ? await dbCmd.ExecuteReaderAsync(cancellationToken) : command.ExecuteReader())
            {
                var results = new List<T>();
                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    while (reader is DbDataReader dbReader ? await dbReader.ReadAsync(cancellationToken) : reader.Read())
                    {
                        var value = reader.GetValue(0);
                        results.Add(reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
                    }
                }
                else
                {
                    var mapper = CreateMapper<T>(reader);
                    while (reader is DbDataReader dbReader ? await dbReader.ReadAsync(cancellationToken) : reader.Read())
                    {
                        var item = Activator.CreateInstance<T>();
                        mapper(item, reader);
                        results.Add(item);
                    }
                }
                return results;
            }
        }
    }

    public static T QueryFirstOrDefault<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            EnsureOpenConnection(connection);

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                    return default;

                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    var value = reader.GetValue(0);
                    return reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
                else
                {
                    var item = Activator.CreateInstance<T>();
                    var mapper = CreateMapper<T>(reader);
                    mapper(item, reader);
                    return item;
                }
            }
        }
    }

    public static async Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection connection, string sql, object param = null,
        IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            await EnsureOpenConnectionAsync(connection, cancellationToken);

            using (var reader = command is DbCommand dbCmd ? await dbCmd.ExecuteReaderAsync(cancellationToken) : command.ExecuteReader())
            {
                if (!(reader is DbDataReader dbReader ? await dbReader.ReadAsync(cancellationToken) : reader.Read()))
                    return default;

                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    var value = reader.GetValue(0);
                    return reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
                else
                {
                    var item = Activator.CreateInstance<T>();
                    var mapper = CreateMapper<T>(reader);
                    mapper(item, reader);
                    return item;
                }
            }
        }
    }

    public static T QuerySingleOrDefault<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            EnsureOpenConnection(connection);

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                    return default;

                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    var value = reader.GetValue(0);
                    return reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
                else
                {
                    var item = Activator.CreateInstance<T>();
                    var mapper = CreateMapper<T>(reader);
                    mapper(item, reader);
                    return reader.Read() ? default : item;
                }
            }
        }
    }

    public static async Task<T> QuerySingleOrDefaultAsync<T>(this IDbConnection connection, string sql, object param = null,
        IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, param);
            await EnsureOpenConnectionAsync(connection, cancellationToken);

            using (var reader = command is DbCommand dbCmd ? await dbCmd.ExecuteReaderAsync(cancellationToken) : command.ExecuteReader())
            {
                bool hasRow = reader is DbDataReader dbReader ? await dbReader.ReadAsync(cancellationToken) : reader.Read();
                if (!hasRow)
                    return default;

                if (typeof(T).IsValueType || typeof(T) == typeof(string) || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    var value = reader.GetValue(0);
                    return reader.IsDBNull(0) ? default : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
                else
                {
                    var item = Activator.CreateInstance<T>();
                    var mapper = CreateMapper<T>(reader);
                    mapper(item, reader);
                    bool hasMoreRows = reader is DbDataReader dbRdr ? await dbRdr.ReadAsync(cancellationToken) : reader.Read();
                    return hasMoreRows ? default : item;
                }
            }
        }
    }

    public static int Execute(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int commandTimeout = DefaultTimeout)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandTimeout = Math.Max(0, commandTimeout);
            AddParameters(command, param);
            EnsureOpenConnection(connection);
            return command.ExecuteNonQuery();
        }
    }

    public static async Task<int> ExecuteAsync(this IDbConnection connection, string sql, object param = null,
        IDbTransaction transaction = null, int commandTimeout = DefaultTimeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandTimeout = Math.Max(0, commandTimeout);
            AddParameters(command, param);
            await EnsureOpenConnectionAsync(connection, cancellationToken);
            return command is DbCommand dbCmd ? await dbCmd.ExecuteNonQueryAsync(cancellationToken) : command.ExecuteNonQuery();
        }
    }

    private static void AddParameters(IDbCommand command, object param)
    {
        if (param == null)
            return;

        foreach (var prop in param.GetType().GetProperties())
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{prop.Name}";
            parameter.Value = prop.GetValue(param) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    private static Action<T, IDataReader> CreateMapper<T>(IDataReader reader)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        return (obj, r) =>
        {
            for (int i = 0; i < r.FieldCount; i++)
            {
                var columnName = r.GetName(i);
                if (properties.TryGetValue(columnName, out var prop))
                {
                    if (r.IsDBNull(i))
                    {
                        prop.SetValue(obj, null);
                    }
                    else
                    {
                        var value = r.GetValue(i);
                        try
                        {
                            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            var convertedValue = Convert.ChangeType(value, targetType);
                            prop.SetValue(obj, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to map {columnName} to {prop.Name}: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
        };
    }
}