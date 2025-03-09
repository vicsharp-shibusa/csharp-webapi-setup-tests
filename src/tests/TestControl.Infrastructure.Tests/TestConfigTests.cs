namespace TestControl.Infrastructure.Tests;

public class TestConfigTests
{
    [Fact]
    public void Serialization_NonDefault_Config()
    {
        var testFilePath = Path.Combine("data", "non-default-config.json");

        var sut = TestConfig.LoadFromFile(testFilePath);

        Assert.NotNull(sut);

        // Assert top-level properties
        Assert.Equal("http://production:9000", sut.ApiBaseUrl);
        Assert.Equal("Test.Gamma", sut.ApiName);
        Assert.Equal("MySQL", sut.DatabaseEngine);
        Assert.Equal("db3", sut.DatabaseVersion);
        Assert.Equal(3000, sut.ShutdownDelayMs);
        Assert.Equal(10, sut.TestDurationMinutes);
        Assert.Equal(2000, sut.MaxAdmins);
        Assert.Equal(25, sut.WarmupCycles);

        // Assert AdminGrowth properties
        Assert.Equal(5, sut.AdminGrowth.InitialAdminCount);
        Assert.Equal(4, sut.AdminGrowth.InitialOrgCount);
        Assert.Equal(3, sut.AdminGrowth.InitialParentOrgCount);
        Assert.Equal(10, sut.AdminGrowth.InitialWorkerCountPerOrg);
        Assert.Equal(3, sut.AdminGrowth.AdminGrowthPerAdmin);

        // Assert AdminReporting properties
        Assert.Equal(5, sut.AdminReporting.ReportsToRunPerCycle);

        // Assert FrequencyControl properties
        Assert.Equal(40, sut.FrequencyControl.AdminGrowthCycleSeconds);

        // Assert AdminQueries properties within FrequencyControl
        Assert.Equal(60, sut.FrequencyControl.AdminQueries.InitialFrequencySeconds);
        Assert.Equal(15, sut.FrequencyControl.AdminQueries.MinFrequencySeconds);
        Assert.Equal(20, sut.FrequencyControl.AdminQueries.MaxTimeToMinFrequencyMinutes);

        // Assert TransactionProcessing properties within FrequencyControl
        Assert.Equal(90, sut.FrequencyControl.TransactionProcessing.InitialFrequencySeconds);
        Assert.Equal(20, sut.FrequencyControl.TransactionProcessing.MinFrequencySeconds);
        Assert.Equal(25, sut.FrequencyControl.TransactionProcessing.MaxTimeToMinFrequencyMinutes);

        // Assert FailureHandling properties
        Assert.Equal(75, sut.FailureHandling.PeriodAverageResponseTime);
        Assert.Equal(750, sut.FailureHandling.AverageResponseTimeThresholdMs);
    }

}
