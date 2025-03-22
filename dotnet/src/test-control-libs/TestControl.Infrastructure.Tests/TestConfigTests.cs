using System.Text.Json;

namespace TestControl.Infrastructure.Tests;

public class TestConfigTests
{
    [Theory]
    [InlineData("default-config.json")]
    [InlineData("non-default-config.json")]
    public void Serialization_Config(string filename)
    {
        var testFilePath = Path.Combine("data", filename);

        var sut = TestConfig.LoadFromFile(testFilePath);

        Assert.NotNull(sut);
    }

    [Fact]
    public void CreateDefaultConfigJson()
    {
        var sut = new TestConfig();
        var json = JsonSerializer.Serialize(sut, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        Assert.NotNull(json);
    }
}
