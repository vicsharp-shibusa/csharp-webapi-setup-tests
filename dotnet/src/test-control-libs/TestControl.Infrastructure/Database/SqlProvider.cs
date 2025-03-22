namespace TestControl.Infrastructure.Database;

/// <summary>
/// Represents a wrapper for a dictionary containing the SQL for the data access layer.
/// Based on db engine and version, this is instantiated on startup and compiles the
/// collection of necessary SQL to make the test run. 
/// </summary>
public class SqlProvider
{
    private readonly IReadOnlyDictionary<string, string> _sqlStatements;

    public SqlProvider(IReadOnlyDictionary<string, string> sqlQueries)
    {
        _sqlStatements = sqlQueries ?? throw new ArgumentNullException(nameof(sqlQueries));
    }

    /// <summary>
    /// Gets the SQL for a given key.
    /// </summary>
    /// <param name="key">The key for the SQL. See the <see cref="SqlKeys"/> class.</param>
    /// <returns>A string containing the SQL for the tests' specified db engine and version.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if key is not present in the dictionary.</exception>
    public string GetSql(string key)
    {
        if (_sqlStatements.TryGetValue(key, out var statement))
            return statement;
        throw new KeyNotFoundException($"No SQL found for key '{key}'.");
    }
}
