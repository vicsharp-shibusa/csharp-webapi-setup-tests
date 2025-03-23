using Alpha.Common;
using Microsoft.AspNetCore.Mvc;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Alpha.WebApi.Controllers;

[ApiController]
public class TestController : ControllerBase
{
    private const string TestName = "Test.Alpha";
    private readonly TestMetricsService _testMetricsService;
    private readonly IUserRepository _userRepository;
    private readonly DbMaintenanceServiceScoped _dbMaintenanceService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        TestMetricsService testMetricsService,
        IUserRepository userRepository,
        DbMaintenanceServiceScoped dbMaintenanceService,
        ILogger<TestController> logger)
    {
        _testMetricsService = testMetricsService;
        _userRepository = userRepository;
        _dbMaintenanceService = dbMaintenanceService;
        _logger = logger;
    }

    [HttpPost(Constants.TestUris.Reset)]
    [HttpPost(Constants.TestUris.TestInitialize)]
    public async Task<IActionResult> Initialize()
    {
        try
        {
            var t = _dbMaintenanceService.PurgeDatabase();
            _testMetricsService.Reset();
            await t;
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Database purge failed.", details = ex.Message });
        }
    }

    [HttpPost(Constants.TestUris.TestWarmup)]
    public async Task<IActionResult> Warmup([FromBody] TestConfig config)
    {
        try
        {
            if (config == null)
                return BadRequest(new { error = "Invalid configuration provided." });

            int warmupCycles = config.WarmupCycles > 0 ? config.WarmupCycles : 10;

            for (int i = 0; i < warmupCycles; i++)
            {
                var operationId = Guid.NewGuid();
                var user = TestDataCreationService.CreateUser();

                await _userRepository.UpsertAsync(user, operationId);
            }

            return await Initialize();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error in {nameof(Warmup)}");
            return StatusCode(500, new { error = "Warmup failed.", details = ex.Message });
        }
    }

    [HttpGet(Constants.TestUris.Status)]
    public async Task<IActionResult> GetTestStatusAsync([FromQuery] int responseTimeThreshold = 999)
    {
        var status = new TestStatus()
        {
            ResponseTimeThreshold = responseTimeThreshold,
            DbCounts = await _dbMaintenanceService.CountRows(),
            ServiceInstantiations = _testMetricsService.GetInstantiationCounts(),
        };

        return Ok(status);
    }

    [HttpGet(Constants.TestUris.Name)]
    public IActionResult GetApiStatus()
    {
        var version = Environment.GetEnvironmentVariable(Constants.EnvironmentVariableNames.DbVersion) ?? "Unknown";
        return Ok(new ApiInfo(true, TestName, version, ""));
    }
}
