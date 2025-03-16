using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.Infrastructure;

public delegate void MessageHandler(MessageToControlProgram message);
public delegate TestStatus StatusHandler(TestStatus statusFromServer);

public enum MessageLevel
{
    Info,
    Warning,
    Critical,
    Error
}

public enum DbEngine
{
    PostgreSQL = 0,
    MSSQL = 1
}

public enum Mode
{
    Fair = 0,
    BruteForce = 1
}

public enum UserTransactionType
{
    Pending = 0,
    Approved,
    Denied,
    Cancelled
}

public static class Constants
{
    public static class WorkerTypes
    {
        public const string Admin = nameof(Admin);
        public const string Worker = nameof(Worker);
    }

    public static class TestUris
    {
        public const string TestInitialize = "api/test/initialize";
        public const string TestWarmup = "api/test/warmup";
        public const string Status = "api/status";
        public const string Name = "api/name";
        public const string Reset = "api/maintenance/reset";
    }

    public static class EnvironmentVariableNames
    {
        public const string DbEngine = "DB_ENGINE";
        public const string DbVersion = "DB_VERSION";
    }
}
