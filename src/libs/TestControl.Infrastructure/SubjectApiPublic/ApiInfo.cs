namespace TestControl.Infrastructure.SubjectApiPublic;

public record ApiInfo
{
    public ApiInfo(bool isRunning, string serverName, string dbVersion, string message)
    {
        IsRunning = isRunning;
        ServerName = serverName;
        DbVersion = dbVersion;
        Message = message;
    }

    public bool IsRunning { get; init; }
    public string ServerName { get; init; }
    public string DbVersion { get; init; }
    public string Message { get; init; }

    public bool VersionsMatch(string name, string dbVersion) => (name?.Equals(ServerName, StringComparison.OrdinalIgnoreCase) ?? false) &&
        (dbVersion?.Equals(DbVersion, StringComparison.OrdinalIgnoreCase) ?? false);
}
