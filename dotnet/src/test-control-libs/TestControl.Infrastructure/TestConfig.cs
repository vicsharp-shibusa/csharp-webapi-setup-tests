﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestControl.Infrastructure;

/// <summary>
/// Represents the configuraiton of the test being conducted.
/// </summary>
public class TestConfig
{
    [JsonPropertyName("mode")]
    public string TestMode { get; init; } = Mode.Fair.ToString();
    private Mode _mode = Mode.Fair;
    [JsonIgnore]
    public Mode Mode
    {
        get
        {
            if (!Enum.TryParse(TestMode.Replace(" ", ""), true, out _mode))
            {
                throw new Exception($"Unknown test mode: {TestMode}");
            }

            return _mode;
        }
    }
    public int MinDelayMs { get; init; } = 0;
    [JsonIgnore]
    public TimeSpan MinDelay => MinDelayMs <= 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(MinDelayMs);
    public int TestDurationMinutes { get; init; } = 0;
    public int ShutdownDelayMs { get; init; } = 10_000;
    public int WarmupCycles { get; init; } = 500;
    public int StatusCheckIntervalSeconds { get; init; } = 30;
    public ApiConfig Api { get; init; } = new();
    public AdminConfig Admins { get; init; } = new();
    public WorkerConfig Workers { get; init; } = new();
    public ResponseThresholdConfig ResponseThreshold { get; init; } = new();

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static TestConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<TestConfig>(json, _serializerOptions)
            ?? throw new Exception("Failed to deserialize configuration.");

        var messages = config.GetValidationMessages().ToArray();

        if (messages.Length > 0)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Config file failed validation checks on {nameof(LoadFromFile)}");
            foreach (var m in messages)
            {
                sb.AppendLine(m);
            }
            throw new ArgumentException(sb.ToString());
        }

        return config;
    }

    public IEnumerable<string> GetValidationMessages()
    {
        const int MaxDur = 72 * 60; // 3 days
        if (TestDurationMinutes < 0)
            yield return ConfigMessageHandler.LessThanMessage(nameof(TestDurationMinutes), 0);
        if (TestDurationMinutes > MaxDur)
            yield return ConfigMessageHandler.GreaterThanMessage(nameof(TestDurationMinutes), MaxDur);
        if (ShutdownDelayMs < 0)
            yield return ConfigMessageHandler.LessThanMessage(nameof(ShutdownDelayMs), 0);
        if (WarmupCycles < 0)
            yield return ConfigMessageHandler.LessThanMessage(nameof(WarmupCycles), 0);
        if (StatusCheckIntervalSeconds < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(StatusCheckIntervalSeconds), 1);
        if (MinDelayMs < 0)
            yield return ConfigMessageHandler.LessThanMessage(nameof(MinDelayMs), 0);

        foreach (var item in Api.GetValidationMessages()
            .Union(Admins.GetValidationMessages())
            .Union(Workers.GetValidationMessages())
            .Union(ResponseThreshold.GetValidationMessages()))
            yield return item;
    }
}

public class ApiConfig
{
    public string ApiBaseUrl { get; init; } = "https://localhost:5260";
    public string ApiName { get; init; } = "Test.Alpha";
    public string DatabaseEngine { get; init; } = DbEngine.PostgreSQL.ToString();
    public string DatabaseVersion { get; init; } = "db1";
    public IEnumerable<string> GetValidationMessages()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            yield return ConfigMessageHandler.GetMissingMessage(nameof(ApiBaseUrl));
        if (string.IsNullOrWhiteSpace(ApiName))
            yield return ConfigMessageHandler.GetMissingMessage(nameof(ApiName));
        if (string.IsNullOrWhiteSpace(DatabaseEngine))
            yield return ConfigMessageHandler.GetMissingMessage(nameof(DatabaseEngine));
        if (string.IsNullOrWhiteSpace(DatabaseVersion))
            yield return ConfigMessageHandler.GetMissingMessage(nameof(DatabaseVersion));
    }
}

public class AdminConfig
{
    public int MaxAdmins { get; init; } = 1_000;
    public int InitialAdmins { get; init; } = 1;
    public int InitialParentOrgsPerAdmin { get; init; } = 1;
    public int InitialOrgsPerParent { get; init; } = 1;
    public int InitialWorkersPerOrg { get; init; } = 2;
    public int AdminGrowthPerAdmin { get; init; } = 1;
    public int AdminGrowthCycleTimeLimitSeconds { get; init; } = 20;
    public double AdminGrowthCycleFrequencyMs { get; init; } = 15_000;
    public int ReportsToRunPerCycle { get; init; } = 1;
    public RateOfChangeConfig AdminQueryRoc { get; init; } = new();

    public IEnumerable<string> GetValidationMessages()
    {
        if (MaxAdmins < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(MaxAdmins), 1);
        if (InitialAdmins < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(InitialAdmins), 1);
        if (MaxAdmins < InitialAdmins)
            yield return $"{nameof(MaxAdmins)} cannot be less than {nameof(InitialAdmins)}";
        if (InitialOrgsPerParent < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(InitialOrgsPerParent), 1);
        if (InitialParentOrgsPerAdmin < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(InitialParentOrgsPerAdmin), 1);
        if (InitialWorkersPerOrg < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(InitialWorkersPerOrg), 1);
        if (AdminGrowthPerAdmin < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(AdminGrowthPerAdmin), 1);
        if (AdminGrowthCycleTimeLimitSeconds < 1D)
            yield return ConfigMessageHandler.LessThanMessage(nameof(AdminGrowthCycleTimeLimitSeconds), 1D);
        if (AdminGrowthCycleFrequencyMs < 1D)
            yield return ConfigMessageHandler.LessThanMessage(nameof(AdminGrowthCycleFrequencyMs), 1D);
        if (ReportsToRunPerCycle < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(ReportsToRunPerCycle), 1);
        foreach (var item in AdminQueryRoc.GetValidationMessages(nameof(AdminConfig)))
            yield return item;
    }

    /*
     * A rough estimate of the number of API calls necessary for a single admin growth cycle.
     */
    [JsonIgnore]
    public int UnitsOfWorkEstimate => InitialParentOrgsPerAdmin * InitialAdmins * InitialOrgsPerParent * (1 + InitialWorkersPerOrg);
}

public class WorkerConfig
{
    public int TransactionsToCreatePerCycle { get; init; } = 1;
    public int TransactionsToEvaluatePerCycle { get; init; } = 1;
    public int WorkerCycleTimeLimitSeconds { get; init; } = 20;
    public RateOfChangeConfig WorkerTransactionsRoc { get; init; } = new();

    public IEnumerable<string> GetValidationMessages()
    {
        if (TransactionsToCreatePerCycle < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(TransactionsToCreatePerCycle), 1);
        if (TransactionsToEvaluatePerCycle < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(TransactionsToEvaluatePerCycle), 1);
        if (WorkerCycleTimeLimitSeconds < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(WorkerCycleTimeLimitSeconds), 1);
        foreach (var item in WorkerTransactionsRoc.GetValidationMessages(nameof(WorkerConfig)))
            yield return item;
    }
}

public class RateOfChangeConfig
{
    public int InitialFrequencySeconds { get; init; } = 30;
    public int MinFrequencySeconds { get; init; } = 1;
    public int FrequencyToDecreaseIntervalSeconds { get; init; } = 10;
    public int AmountToDecreaseMs { get; init; } = 500;

    public IEnumerable<string> GetValidationMessages(string callerName)
    {
        if (InitialFrequencySeconds < 1)
            yield return ConfigMessageHandler.LessThanMessage($"{callerName}.{nameof(InitialFrequencySeconds)}", 1);
        if (MinFrequencySeconds < 0)
            yield return ConfigMessageHandler.LessThanMessage($"{callerName}.{nameof(MinFrequencySeconds)}", 0);
        if (FrequencyToDecreaseIntervalSeconds < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(FrequencyToDecreaseIntervalSeconds), 1);
        if (AmountToDecreaseMs < 1)
            yield return ConfigMessageHandler.LessThanMessage($"{callerName}.{nameof(AmountToDecreaseMs)}", 1);
    }
}

public class ResponseThresholdConfig
{
    public int AverageResponseTimePeriod { get; init; } = 100;
    public int AverageResponseTimeThresholdMs { get; init; } = 999;

    public IEnumerable<string> GetValidationMessages()
    {
        if (AverageResponseTimePeriod < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(AverageResponseTimePeriod), 1);
        if (AverageResponseTimeThresholdMs < 1)
            yield return ConfigMessageHandler.LessThanMessage(nameof(AverageResponseTimeThresholdMs), 1);
    }
}

file static class ConfigMessageHandler
{
    public static string GetMissingMessage(string propertyName) =>
        $"{propertyName} is missing, null, or invalid.";
    public static string LessThanMessage(string propertyName, int value) =>
        $"{propertyName} cannot be less than {value}";
    public static string LessThanMessage(string propertyName, double value) =>
        $"{propertyName} cannot be less than {value:F2}";
    public static string GreaterThanMessage(string propertyName, int value) =>
        $"{propertyName} cannot be greater than {value}";
}
