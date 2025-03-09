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

public static class Constants
{
    public static class TestUris
    {
        public const string TestInitialize = "api/test/initialize";
        public const string TestWarmup = "api/test/warmup";
        //public const string Org = "api/org";
        public const string Status = "api/status";
        public const string Name = "api/name";
        public const string Reset = "api/maintenance/reset";
        //public const string User = "api/user";
        //public const string Users = "api/users";
    }

    public static class EnvironmentVariableNames
    {
        public const string DbEngine = "DB_ENGINE";
        public const string DbVersion = "DB_VERSION";
    }

    public static class DirectoryNames
    {
        public const string TestSubjectDirectoryName = "subjects";
        public const string ProjectRootName = "src";
    }
}
