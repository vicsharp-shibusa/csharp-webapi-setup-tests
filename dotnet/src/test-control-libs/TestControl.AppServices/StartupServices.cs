using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;

namespace TestControl.AppServices;

public static class StartupServices
{
    public static SqlProvider GetSqlProvider(string engine, int version)
    {
        if (!Enum.TryParse<DbEngine>(engine, out var dbEngine))
        {
            throw new ArgumentException($"Could not parse db engine: {engine}");
        }
        return new SqlProvider(new SqlRepository().BuildDictionary(dbEngine, Math.Max(1, version)));
    }
}
