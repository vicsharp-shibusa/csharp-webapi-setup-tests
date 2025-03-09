namespace TestControl.Infrastructure.Database;

public class SqlProvider
{
    private readonly IReadOnlyDictionary<string, string> _sqlStatements;

    public SqlProvider(IReadOnlyDictionary<string, string> sqlQueries)
    {
        _sqlStatements = sqlQueries ?? throw new ArgumentNullException(nameof(sqlQueries));
    }

    public string GetSql(string key)
    {
        if (_sqlStatements.TryGetValue(key, out var statement))
            return statement;
        throw new KeyNotFoundException($"No SQL found for key '{key}'.");
    }
}
