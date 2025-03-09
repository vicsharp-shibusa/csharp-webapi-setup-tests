using System.Text.Json;

namespace TestControl.Infrastructure;

/// <summary>
/// Represents the parameters of the test being conducted.
/// </summary>
public class TestConfig
{
    /// <summary>
    /// Gets the base URL for the API being tested.
    /// </summary>
    public string ApiBaseUrl { get; init; } = "http://localhost:5259";
    /// <summary>
    /// Gets the expected name of the API being tested.
    /// </summary>
    public string ApiName { get; init; } = "Test.Alpha";
    /// <summary>
    /// Gets the database engine.
    /// </summary>
    public string DatabaseEngine { get; init; } = DbEngine.PostgreSQL.ToString();
    /// <summary>
    /// Gets the version of the database. Should be in format of "dbX" where X is a positive integer.
    /// </summary>
    public string DatabaseVersion { get; init; } = "db1";
    /// <summary>
    /// Gets the delay in ms when an unexpected interrupt occurs.
    /// </summary>
    public int ShutdownDelayMs { get; init; } = 1_000;
    /// <summary>
    /// Gets the total number of minutes the test can run.
    /// </summary>
    public int TestDurationMinutes { get; init; } = 1;
    /// <summary>
    /// Gets the total number of seconds in the interval between status checks.
    /// </summary>
    public int StatusCheckIntervalSeconds { get; init; } = 30;
    /// <summary>
    /// Gets the maximum number of Admin instances to be created.
    /// </summary>
    public int MaxAdmins { get; init; } = 1000;
    /// <summary>
    /// Gets the number of cycles performed for the test warmup.
    /// </summary>
    public int WarmupCycles { get; init; } = 10;
    /// <summary>
    /// Gets Admin growth configuration.
    /// </summary>
    public AdminGrowthConfig AdminGrowth { get; init; } = new();
    /// <summary>
    /// Gets configuration for the admin reporting cycle.
    /// </summary>
    public AdminReportingConfig AdminReporting { get; init; } = new();
    /// <summary>
    /// Gets frequency control values.
    /// </summary>
    public FrequencyControlConfig FrequencyControl { get; init; } = new();
    /// <summary>
    /// Gets the configuration for when to stop the test.
    /// </summary>
    public FailureHandlingConfig FailureHandling { get; init; } = new();

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads a test configuration file into memory.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    /// <returns>A <see cref="TestConfig"/> instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when path is bad.</exception>
    /// <exception cref="Exception">Thrown when file's content cannot be
    /// serialized into a <see cref="TestConfig"/> instance.</exception>
    public static TestConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TestConfig>(json, SerializerOptions)
            ?? throw new Exception("Failed to deserialize configuration.");
    }

    private static string GetMissingMessage(string propertyName) =>
        $"{propertyName} is missing, null, or invalid.";

    public IEnumerable<string> GetValidationMessages()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            yield return GetMissingMessage(nameof(ApiBaseUrl));
        if (string.IsNullOrWhiteSpace(ApiName))
            yield return GetMissingMessage(nameof(ApiName));
        if (string.IsNullOrWhiteSpace(DatabaseEngine))
            yield return GetMissingMessage(nameof(DatabaseEngine));
        if (string.IsNullOrWhiteSpace(DatabaseVersion))
            yield return GetMissingMessage(nameof(DatabaseVersion));
        if (ShutdownDelayMs < 0)
            yield return $"{nameof(ShutdownDelayMs)} cannot be less than zero.";
        if (MaxAdmins < 1)
            yield return $"{nameof(MaxAdmins)} cannot be less than 1.";
        if (WarmupCycles < 0)
            yield return $"{nameof(WarmupCycles)} cannot be less than 0.";

        foreach (var item in AdminGrowth.GetValidationMessages())
            yield return item;
        foreach (var item in AdminReporting.GetValidationMessages())
            yield return item;
        foreach (var item in FrequencyControl.GetValidationMessages())
            yield return item;
        foreach (var item in FailureHandling.GetValidationMessages())
            yield return item;
    }
}

public class AdminGrowthConfig
{
    /// <summary>
    /// Gets the number of admins created immediately.
    /// </summary>
    public int InitialAdminCount { get; init; } = 1;
    /// <summary>
    /// Gets the number of orgs created per admin instantiation.
    /// </summary>
    public int InitialOrgCount { get; init; } = 1;
    /// <summary>
    /// Gets the number of parent orgs created per admin instantiation.
    /// </summary>
    public int InitialParentOrgCount { get; init; } = 1;
    /// <summary>
    /// Gets the number of workers created per org (not parent orgs).
    /// </summary>
    public int InitialWorkerCountPerOrg { get; init; } = 2;
    /// <summary>
    /// Gets the number of new admins for each existing admin.
    /// </summary>
    public int AdminGrowthPerAdmin { get; init; } = 1;

    public IEnumerable<string> GetValidationMessages()
    {
        if (InitialAdminCount < 1)
            yield return $"{nameof(InitialAdminCount)} cannot be less than 1.";
        if (InitialOrgCount < 1)
            yield return $"{nameof(InitialOrgCount)} cannot be less than 1.";
        if (InitialParentOrgCount < 1)
            yield return $"{nameof(InitialParentOrgCount)} cannot be less than 1.";
        if (InitialWorkerCountPerOrg < 1)
            yield return $"{nameof(InitialWorkerCountPerOrg)} cannot be less than 1.";
        if (AdminGrowthPerAdmin < 1)
            yield return $"{nameof(AdminGrowthPerAdmin)} cannot be less than 1.";
    }
}

public class FrequencyControlConfig
{
    /// <summary>
    /// Gets the number of seconds between Admin growth spurts.
    /// </summary>
    public int AdminGrowthCycleSeconds { get; init; } = 20;
    /// <summary>
    /// Gets the rate of change for admin queries.
    /// </summary>
    public FrequencyRateOfChangeConfig AdminQueries { get; init; } = new();
    /// <summary>
    /// Gets the rate of change for user transaction processing.
    /// </summary>
    public FrequencyRateOfChangeConfig TransactionProcessing { get; init; } = new();

    public IEnumerable<string> GetValidationMessages()
    {
        if (AdminGrowthCycleSeconds < 1)
            yield return $"{nameof(AdminGrowthCycleSeconds)} cannot be less than 1.";

        foreach (var item in AdminQueries.GetValidationMessages(nameof(AdminQueries)))
            yield return item;

        foreach (var item in TransactionProcessing.GetValidationMessages(nameof(TransactionProcessing)))
            yield return item;
    }
}


public class AdminReportingConfig
{
    /// <summary>
    /// Gets the number of reports an Admin should run during the reporting cycle.
    /// </summary>
    public int ReportsToRunPerCycle { get; init; } = 1;

    public IEnumerable<string> GetValidationMessages()
    {
        if (ReportsToRunPerCycle < 1)
            yield return $"{nameof(ReportsToRunPerCycle)} cannot be less than 1.";
    }
    public bool IsValid => !GetValidationMessages().Any();
}

public class TransactionProcessingConfig
{
    /// <summary>
    /// Gets the number of transactions a worker should create per cycle.
    /// </summary>
    public int UserTransactionsToCreatePerCycle { get; init; } = 1;
    /// <summary>
    /// Gets the number of transactions a worker should review per cycle.
    /// </summary>
    public int UserTransactionsToReviewPerCycle { get; init; } = 1;

    public IEnumerable<string> GetValidationMessages()
    {
        if (UserTransactionsToCreatePerCycle < 1)
            yield return $"{nameof(UserTransactionsToCreatePerCycle)} cannot be less than 1.";
        if (UserTransactionsToReviewPerCycle < 1)
            yield return $"{nameof(UserTransactionsToReviewPerCycle)} cannot be less than 1.";
    }
}

public class FrequencyRateOfChangeConfig
{
    /// <summary>
    /// Gets the initial number of seconds allotted to a cycle.
    /// </summary>
    public int InitialFrequencySeconds { get; init; } = 30;
    /// <summary>
    /// Gets the smallest number of seconds allotted to a cycle.
    /// </summary>
    public int MinFrequencySeconds { get; init; } = 5;
    /// <summary>
    /// Gets the number of minutes it takes to go from the InitialFrequencySeconds to MinFrequencySeconds.
    /// </summary>
    public int MaxTimeToMinFrequencyMinutes { get; init; } = 10;

    public IEnumerable<string> GetValidationMessages(string callerName)
    {
        if (InitialFrequencySeconds < 1)
            yield return $"{callerName}.{nameof(InitialFrequencySeconds)} cannot be less than 1.";
        if (MinFrequencySeconds < 1)
            yield return $"{callerName}.{nameof(MinFrequencySeconds)} cannot be less than 1.";
        if (MaxTimeToMinFrequencyMinutes < 1)
            yield return $"{callerName}.{nameof(MaxTimeToMinFrequencyMinutes)} cannot be less than 1.";
    }
}

public class FailureHandlingConfig
{
    /// <summary>
    /// The number of items considered in the response time threshold average calculation.
    /// </summary>
    public int PeriodAverageResponseTime { get; init; } = 100;
    /// <summary>
    /// The threshold for average response time (in ms) over the specified period.
    /// </summary>
    public int AverageResponseTimeThresholdMs { get; init; } = 999;

    public IEnumerable<string> GetValidationMessages()
    {
        if (PeriodAverageResponseTime < 1)
            yield return $"{nameof(PeriodAverageResponseTime)} cannot be less than 1.";
        if (AverageResponseTimeThresholdMs < 1)
            yield return $"{nameof(AverageResponseTimeThresholdMs)} cannot be less than 1.";
    }
}


